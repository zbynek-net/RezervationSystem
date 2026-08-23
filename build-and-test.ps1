<#
.SYNOPSIS
    Builds the ReservationSystem web project and the xUnit test project, then runs the tests.

.DESCRIPTION
    Restores and builds the whole solution (ReservationSystem web app + xUnit test project, both
    targeting .NET Framework 4.8), then runs the tests. You can also just open the solution in
    Visual Studio and build / run tests from Test Explorer - this script only makes a clean
    command-line build reproducible.

.NOTES
    Requires: Visual Studio (any edition) with MSBuild, and NuGet on PATH for restore.
#>

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$sln = Join-Path $root "ReservationSystem.sln"

# --- locate MSBuild via vswhere (works regardless of VS edition) ---
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found - is Visual Studio installed?" }
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
if (-not $msbuild) { throw "MSBuild not found via vswhere." }
Write-Host "MSBuild: $msbuild"

# --- 1) restore + build the whole solution (both projects target .NET Framework 4.8) ---
Write-Host "`n=== Building solution ===" -ForegroundColor Cyan
& $msbuild $sln -restore /p:Configuration=Debug "/p:Platform=Any CPU" /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Solution build failed." }

# --- 2) run the xUnit tests via the console runner from the NuGet cache ---
Write-Host "`n=== Running xUnit tests ===" -ForegroundColor Cyan
$testDll = Join-Path $root "Tests\ReservationSystem.Tests\bin\Debug\net48\ReservationSystem.Tests.dll"
$runner = Join-Path $env:USERPROFILE ".nuget\packages\xunit.runner.console\2.4.2\tools\net472\xunit.console.exe"
if (-not (Test-Path $runner)) { throw "xunit.console.exe not found at $runner - run a restore first." }
& $runner $testDll -nologo
if ($LASTEXITCODE -ne 0) { throw "Tests failed." }

Write-Host "`nAll good: build succeeded and all tests passed." -ForegroundColor Green
