# RuneLite Frame Interpolation Overlay

## This is very very early days and is just a fun thing I'm working on; don't use it as a final product. It's currently in active development, and I'm taking measurements of results that I will post in a table below.

A Windows companion overlay for RuneLite that creates an additional displayed image between captured client frames. It captures the RuneLite window, estimates pixel motion between consecutive images with OpenCV Farnebäck optical flow, warps both images toward an estimated midpoint, and displays the interpolated image in a transparent overlay.

The development launcher runs the interpolation overlay alongside the **RuneLite Hitch Profiler** plugin in one RuneLite client. The profiler acts as a performance reviewer: it records RuneLite frame-time hitches and related client context while you compare sessions with the interpolation overlay enabled or disabled. It measures client frame callbacks rather than exact monitor presentation timing or the overlay's generated-image rate.

## Requirements

- Windows 10 or 11
- JDK 11 or newer
- .NET 9 SDK

## Build

Publish the overlay companion:

```powershell
cd companion\FrameInterpolationOverlay
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The executable is created at:

```text
companion\FrameInterpolationOverlay\bin\Release\net9.0-windows\win-x64\publish\FrameInterpolationOverlay.exe
```

Build the RuneLite plugin:

```powershell
cd ..\..
.\gradlew.bat build
```

## Configure

1. In RuneLite, enable **Frame Interpolation Overlay (Experimental)**.
2. Set **Overlay executable** to the full path of `FrameInterpolationOverlay.exe`.
3. Restart the plugin. It starts the companion while enabled and closes it when disabled.
4. Optionally configure the overlay toggle hotkey.

The overlay captures the complete RuneLite client window. Its image processing includes both the game scene and client interface.

## Run with RuneLite Hitch Profiler

Build RuneLite Hitch Profiler separately from its own project folder with `.\gradlew.bat jar`. The combined launcher uses its standalone JAR from the temporary build directory; it does not copy or compile profiler source in this project.

From this project folder, run:

```powershell
.\gradlew.bat runClientWithHitchProfiler --no-daemon
```

This starts one RuneLite development client with both **Frame Interpolation Overlay (Experimental)** and **RuneLite Hitch Profiler** loaded. Enable RuneLite Hitch Profiler in the plugin list and open its sidebar panel to record the run. Configure the overlay executable as described above. Close other development RuneLite sessions before starting this combined session.
