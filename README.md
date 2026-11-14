# Image Rotate

## Install and use

Run ImageRotate-Setup.exe on Windows 10 or Windows 11. Setup installs Image Rotate for your Windows account and adds it to the Start menu. Administrator access is not required. You can uninstall it from Windows Settings under Apps.

Open or drop an image, or use Paste image (Ctrl+V) to paste an image, screenshot, or single image file from the clipboard. Choose Rotate left 90° or Rotate right 90°, and select Save as. The default filename ends in -rotated. Choose PNG to preserve transparency. JPEG exports use a white background for transparent pixels.

Image Rotate opens PNG, JPEG, BMP, GIF, and TIFF files and saves PNG, JPEG, BMP, and TIFF files. Animated GIFs and multipage TIFFs open as a single frame.

## Build from source

On Windows 10 or Windows 11, extract the source archive and open PowerShell in the extracted directory. Run:

    powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1

The script uses the Windows .NET Framework C# compiler to create ImageRotate.exe and ImageRotate-Setup.exe in the source directory. The installer embeds the application, so only ImageRotate-Setup.exe is needed for distribution.
