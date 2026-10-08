using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FrameInterpolationOverlay;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new CaptureOverlay(args));
    }
}
