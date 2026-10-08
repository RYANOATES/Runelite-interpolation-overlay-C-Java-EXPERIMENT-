# RuneLite Frame Interpolation Overlay

A Windows companion overlay for RuneLite that creates an additional displayed image between captured client frames. It captures the RuneLite window, estimates pixel motion between consecutive images with OpenCV Farnebäck optical flow, warps both images toward an estimated midpoint, and displays the interpolated image in a transparent overlay.

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
