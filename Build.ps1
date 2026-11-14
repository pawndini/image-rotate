$ErrorActionPreference = 'Stop'
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
Push-Location $PSScriptRoot
try {
    & $csc /nologo /target:winexe /platform:anycpu /optimize+ /win32manifest:ImageRotate.manifest /out:ImageRotate.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ImageRotate.cs
    if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
    & $csc /nologo /target:winexe /platform:anycpu /optimize+ /win32manifest:ImageRotate.manifest /out:ImageRotate-Setup.exe /reference:System.Windows.Forms.dll /reference:Microsoft.CSharp.dll /resource:ImageRotate.exe,ImageRotate.exe Setup.cs
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
} finally {
    Pop-Location
}
