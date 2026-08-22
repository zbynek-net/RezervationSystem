<#
.SYNOPSIS
    Builds the ReservationSystem web project and the xUnit test project, then runs the tests.

.DESCRIPTION
    The web project targets .NET Framework 4.5.2 and the test project targets net48. On machines
    that do not have the exact 4.5.2 targeting pack installed, MSBuild needs a FrameworkPathOverride
    pointing at the newest installed 4.x reference assemblies. That override must NOT be applied to
    the net48 test project (it breaks facade resolution), so the two projects are built separately.

    On a machine with the normal 4.5.2 targeting pack (e.g. a standard Visual Studio install) you can
    also just open the solution and build / run tests from Test Explorer - this script only exists to
    make a clean command-line build reproducible.

.NOTES
    Requires: Visual Studio (any edition) with MSBuild, and NuGet on PATH for restore.
#>

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$sln = Join-Path $root "ReservationSystem.sln"
$webProj = Join-Path $root "WebApplication3\ReservationSystem.csproj"
$testProj = Join-Path $root "Tests\ReservationSystem.Tests\ReservationSystem.Tests.csproj"

# --- locate MSBuild via vswhere (works regardless of VS edition) ---
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) { throw "vswhere.exe not found - is Visual Studio installed?" }
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
if (-not $msbuild) { throw "MSBuild not found via vswhere." }
Write-Host "MSBuild: $msbuild"

# --- pick the newest installed .NET Framework reference assemblies for the 4.5.2 web project ---
$refRoot = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework"
$frameworkOverride = $null
foreach ($v in @("v4.8", "v4.7.2", "v4.6.2", "v4.6.1", "v4.6", "v4.5.2")) {
    $candidate = Join-Path $refRoot $v
    if (Test-Path (Join-Path $candidate "mscorlib.dll")) { $frameworkOverride = $candidate; break }
}
$fpArg = if ($frameworkOverride) { "/p:FrameworkPathOverride=$frameworkOverride" } else { $null }
Write-Host "FrameworkPathOverride: $frameworkOverride"

# --- 1) restore + build the web project (net452) ---
Write-Host "`n=== Building web project (ReservationSystem) ===" -ForegroundColor Cyan
& $msbuild $webProj -restore $fpArg /p:Configuration=Debug /p:Platform=AnyCPU /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Web project build failed." }

# --- 2) restore + build the test project (net48), WITHOUT the framework override ---
Write-Host "`n=== Building test project (ReservationSystem.Tests) ===" -ForegroundColor Cyan
& $msbuild $testProj -restore /p:Configuration=Debug /p:BuildProjectReferences=false /nologo /v:minimal
if ($LASTEXITCODE -ne 0) { throw "Test project build failed." }

# --- 3) run the xUnit tests via the console runner from the NuGet cache ---
Write-Host "`n=== Running xUnit tests ===" -ForegroundColor Cyan
$testDll = Join-Path $root "Tests\ReservationSystem.Tests\bin\Debug\net48\ReservationSystem.Tests.dll"
$runner = Join-Path $env:USERPROFILE ".nuget\packages\xunit.runner.console\2.4.2\tools\net472\xunit.console.exe"
if (-not (Test-Path $runner)) { throw "xunit.console.exe not found at $runner - run a restore first." }
& $runner $testDll -nologo
if ($LASTEXITCODE -ne 0) { throw "Tests failed." }

Write-Host "`nAll good: build succeeded and all tests passed." -ForegroundColor Green
