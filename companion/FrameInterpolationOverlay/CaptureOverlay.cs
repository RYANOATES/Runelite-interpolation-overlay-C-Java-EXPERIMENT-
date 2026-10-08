using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenCvSharp;

namespace FrameInterpolationOverlay;

internal sealed class CaptureOverlay : Form
{
    private const int ExTransparent = 0x20;
    private const int ExToolWindow = 0x80;
    private const int ExNoActivate = 0x08000000;
    private const int RopSourceCopy = 0x00CC0020;
    private const int FlowWidth = 640;
    private readonly CancellationTokenSource cancellation = new();
    private Bitmap? current;
    private bool hidden;

    public CaptureOverlay(string[] args)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        Opacity = 0;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        Shown += (_, _) => _ = Task.Run(() => CaptureLoop(cancellation.Token));
        FormClosed += (_, _) => cancellation.Cancel();
        if (args.Contains("--hidden", StringComparer.OrdinalIgnoreCase)) hidden = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= ExTransparent | ExToolWindow | ExNoActivate;
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!hidden && current != null)
            e.Graphics.DrawImage(current, ClientRectangle);
    }

    private async Task CaptureLoop(CancellationToken token)
    {
        using var previous = new Mat();
        using var previousGray = new Mat();
        while (!token.IsCancellationRequested)
        {
            var hwnd = FindRuneLiteWindow();
            if (hwnd == IntPtr.Zero || !GetClientRect(hwnd, out var rect))
            {
                await Task.Delay(250, token).ConfigureAwait(false);
                continue;
            }

            var width = rect.Right - rect.Left;
            var height = rect.Bottom - rect.Top;
            if (width < 200 || height < 150 || IsIconic(hwnd))
            {
                Publish(null, IntPtr.Zero, default, true);
                await Task.Delay(250, token).ConfigureAwait(false);
                continue;
            }

            using var frame = CaptureClient(hwnd, width, height);
            if (frame == null)
            {
                await Task.Delay(40, token).ConfigureAwait(false);
                continue;
            }

            using var color = BitmapToMat(frame);
            using var small = new Mat();
            Cv2.Resize(color, small, new OpenCvSharp.Size(FlowWidth, Math.Max(1, height * FlowWidth / width)), 0, 0, InterpolationFlags.Area);
            using var gray = new Mat();
            Cv2.CvtColor(small, gray, ColorConversionCodes.BGR2GRAY);

            var changed = !previousGray.Empty() && Cv2.Norm(previousGray, gray, NormTypes.L1) > 0.01;
            Bitmap? generated = null;
            if (changed)
            {
                using var forward = new Mat();
                using var backward = new Mat();
                Cv2.CalcOpticalFlowFarneback(previousGray, gray, forward, 0.5, 2, 9, 2, 5, 1.1, OpticalFlowFlags.None);
                Cv2.CalcOpticalFlowFarneback(gray, previousGray, backward, 0.5, 2, 9, 2, 5, 1.1, OpticalFlowFlags.None);
                generated = Interpolate(previous, small, forward, backward, width, height);
            }

            small.CopyTo(previous);
            gray.CopyTo(previousGray);
            if (generated != null)
            {
                Publish(generated, hwnd, rect, false);
                await Task.Delay(11, token).ConfigureAwait(false);
            }
            Publish((Bitmap)frame.Clone(), hwnd, rect, false);
            await Task.Delay(11, token).ConfigureAwait(false);
        }
    }

    private void Publish(Bitmap? bitmap, IntPtr hwnd, RECT rect, bool shouldHide)
    {
        if (IsDisposed || !IsHandleCreated) { bitmap?.Dispose(); return; }
        try
        {
            BeginInvoke(() =>
            {
                current?.Dispose();
                current = bitmap;
                hidden = shouldHide;
                if (!shouldHide && hwnd != IntPtr.Zero)
                {
                    var topLeft = new POINT { X = rect.Left, Y = rect.Top };
                    ClientToScreen(hwnd, ref topLeft);
                    Bounds = new Rectangle(topLeft.X, topLeft.Y, rect.Right - rect.Left, rect.Bottom - rect.Top);
                    Opacity = 0.99;
                    if (!Visible) Show();
                    SetWindowPos(Handle, HwndTopmost, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, SwpNoActivate | SwpShowWindow);
                }
                else if (Visible) Hide();
                Invalidate();
            });
        }
        catch (InvalidOperationException) { bitmap?.Dispose(); }
    }

    private static Bitmap Interpolate(Mat oldColor, Mat newColor, Mat forward, Mat backward, int width, int height)
    {
        // Half-flow warps approximate the temporal midpoint; blending fills holes.
        using var mapX0 = new Mat(forward.Size(), MatType.CV_32FC1);
        using var mapY0 = new Mat(forward.Size(), MatType.CV_32FC1);
        using var mapX1 = new Mat(backward.Size(), MatType.CV_32FC1);
        using var mapY1 = new Mat(backward.Size(), MatType.CV_32FC1);
        BuildHalfFlowMaps(forward, mapX0, mapY0);
        BuildHalfFlowMaps(backward, mapX1, mapY1);
        using var warped0 = new Mat();
        using var warped1 = new Mat();
        using var blended = new Mat();
        Cv2.Remap(oldColor, warped0, mapX0, mapY0, InterpolationFlags.Linear, BorderTypes.Reflect101);
        Cv2.Remap(newColor, warped1, mapX1, mapY1, InterpolationFlags.Linear, BorderTypes.Reflect101);
        Cv2.AddWeighted(warped0, 0.5, warped1, 0.5, 0, blended);
        using var enlarged = new Mat();
        Cv2.Resize(blended, enlarged, new OpenCvSharp.Size(width, height), 0, 0, InterpolationFlags.Cubic);
        return MatToBitmap(enlarged);
    }

    private static void BuildHalfFlowMaps(Mat flow, Mat mapX, Mat mapY)
    {
        var flowRow = new float[flow.Cols * 2];
        var xRow = new float[flow.Cols];
        var yRow = new float[flow.Cols];
        for (var y = 0; y < flow.Rows; y++)
        {
            Marshal.Copy(flow.Ptr(y), flowRow, 0, flowRow.Length);
            for (var x = 0; x < flow.Cols; x++)
            {
                xRow[x] = x - flowRow[x * 2] * 0.5f;
                yRow[x] = y - flowRow[x * 2 + 1] * 0.5f;
            }
            Marshal.Copy(xRow, 0, mapX.Ptr(y), xRow.Length);
            Marshal.Copy(yRow, 0, mapY.Ptr(y), yRow.Length);
        }
    }

    private static Mat BitmapToMat(Bitmap bitmap)
    {
        using var converted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(converted)) g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
        var data = converted.LockBits(new Rectangle(0, 0, converted.Width, converted.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            var mat = new Mat(converted.Height, converted.Width, MatType.CV_8UC3);
            var row = new byte[converted.Width * 3];
            for (var y = 0; y < converted.Height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                Marshal.Copy(row, 0, mat.Ptr(y), row.Length);
            }
            return mat;
        }
        finally { converted.UnlockBits(data); }
    }

    private static Bitmap MatToBitmap(Mat mat)
    {
        using var bgr = new Mat();
        if (mat.Channels() == 1) Cv2.CvtColor(mat, bgr, ColorConversionCodes.GRAY2BGR); else mat.CopyTo(bgr);
        var bitmap = new Bitmap(bgr.Width, bgr.Height, PixelFormat.Format24bppRgb);
        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
        try
        {
            var row = new byte[bitmap.Width * 3];
            for (var y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(bgr.Ptr(y), row, 0, row.Length);
                Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, row.Length);
            }
        }
        finally { bitmap.UnlockBits(data); }
        return bitmap;
    }

    private static Bitmap? CaptureClient(IntPtr hwnd, int width, int height)
    {
        var src = GetDC(hwnd);
        if (src == IntPtr.Zero) return null;
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        var dst = graphics.GetHdc();
        try
        {
            if (!BitBlt(dst, 0, 0, width, height, src, 0, 0, RopSourceCopy)) { bitmap.Dispose(); return null; }
        }
        finally { graphics.ReleaseHdc(dst); ReleaseDC(hwnd, src); }
        return bitmap;
    }

    private static IntPtr FindRuneLiteWindow()
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hwnd, _) =>
        {
            var title = new System.Text.StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);
            if (title.ToString().Contains("RuneLite", StringComparison.OrdinalIgnoreCase))
            {
                found = hwnd;
                return false;
            }
            GetWindowThreadProcessId(hwnd, out var pid);
            try
            {
                var process = Process.GetProcessById((int)pid);
                if (IsWindowVisible(hwnd) && (process.ProcessName.Equals("RuneLite", StringComparison.OrdinalIgnoreCase)
                    || (process.ProcessName.Equals("java", StringComparison.OrdinalIgnoreCase)
                        && process.MainWindowTitle.Contains("RuneLite", StringComparison.OrdinalIgnoreCase))))
                { found = hwnd; return false; }
            }
            catch { }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { cancellation.Cancel(); current?.Dispose(); cancellation.Dispose(); }
        base.Dispose(disposing);
    }

    private const int HwndTopmost = -1;
    private const uint SwpNoActivate = 0x0010, SwpShowWindow = 0x0040;
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; }
    private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref POINT point);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder text, int maxCount);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd, IntPtr dc);
    [DllImport("user32.dll")] private static extern bool BitBlt(IntPtr dst, int x, int y, int cx, int cy, IntPtr src, int sx, int sy, int rop);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, int insertAfter, int x, int y, int cx, int cy, uint flags);
}
