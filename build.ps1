# DisableVBS build script (no .NET SDK required)
# Usage: powershell -ExecutionPolicy Bypass -File .\build.ps1 [-Out Name.exe]
param([string]$Out = "DisableVBS_v1.0.0-Beta.exe")

$ErrorActionPreference = "Stop"
try { [Console]::OutputEncoding = [Text.Encoding]::UTF8 } catch { }

$base = Split-Path -Parent $MyInvocation.MyCommand.Path
$src  = Join-Path $base "src"
$csc  = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wpf  = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\WPF"
$xaml = "C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll"
$manifest = Join-Path $src "app.manifest"

if (-not (Test-Path $csc)) { Write-Host "csc.exe not found - .NET Framework 4.x required" -ForegroundColor Red; exit 1 }

# Resolve a writable output path (falls back to _2, _3 ... if locked)
$target = Join-Path $base $Out
if (Test-Path $target) {
    $locked = $false
    try { $fs = [IO.File]::Open($target, "Open", "ReadWrite", "None"); $fs.Close() } catch { $locked = $true }
    if ($locked) {
        $stem = [IO.Path]::GetFileNameWithoutExtension($Out)
        $i = 2
        while ($true) {
            $cand = Join-Path $base ("{0}_{1}.exe" -f $stem, $i)
            $free = -not (Test-Path $cand)
            if (-not $free) {
                try { $fs2 = [IO.File]::Open($cand, "Open", "ReadWrite", "None"); $fs2.Close(); $free = $true } catch { $free = $false }
            }
            if ($free) { $target = $cand; break }
            $i++
        }
        Write-Host ("Target in use, writing to: {0}" -f [IO.Path]::GetFileName($target)) -ForegroundColor Yellow
    }
}

$refs = @(
  "/r:$wpf\PresentationFramework.dll",
  "/r:$wpf\PresentationCore.dll",
  "/r:$wpf\WindowsBase.dll",
  "/r:$xaml",
  "/r:System.dll", "/r:System.Drawing.dll", "/r:System.Core.dll"
)
$icon = Join-Path $src "app.ico"
$cscArgs = @("/nologo", "/target:winexe", "/utf8output", "/win32manifest:$manifest", "/out:$target")
if (Test-Path $icon) { $cscArgs += "/win32icon:$icon" }
$cscArgs += $refs
$cscArgs += @((Join-Path $src "Wpf.cs"), (Join-Path $src "Engine.cs"), (Join-Path $src "AssemblyInfo.cs"))

& $csc $cscArgs
if ($LASTEXITCODE -ne 0) { Write-Host "Build FAILED" -ForegroundColor Red; exit 1 }
$fi = Get-Item $target
Write-Host ("Build OK: {0}  {1} bytes" -f $fi.Name, $fi.Length) -ForegroundColor Green
