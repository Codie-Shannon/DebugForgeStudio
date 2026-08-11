param(
    [switch]$AllowPendingReview,
    [switch]$SkipEvidenceCapture
)

Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$RepoRoot=Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

function Resolve-Python {
    foreach ($Name in @('python.exe','python3.exe','python','python3')) {
        $Command=Get-Command $Name -ErrorAction SilentlyContinue
        if ($Command -and $Command.Source -and $Command.Source -notmatch '(?i)\\WindowsApps\\') {
            return $Command.Source
        }
    }

    $Py=Get-Command py.exe -ErrorAction SilentlyContinue
    if ($Py) {
        $Output=@(& $Py.Source -3 -c 'import sys; print(sys.executable)' 2>&1)
        if ($LASTEXITCODE -eq 0) {
            $Candidate=$Output |
                ForEach-Object { "$_".Trim() } |
                Where-Object { $_ -and (Test-Path -LiteralPath $_ -PathType Leaf) } |
                Select-Object -First 1
            if ($Candidate) { return $Candidate }
        }
    }

    throw 'Python 3.11 or later is required.'
}

dotnet restore DebugForgeStudio.sln
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

dotnet build DebugForgeStudio.sln --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

dotnet run `
    --configuration Release `
    --no-build `
    --project .\tests\DebugForgeStudio.Tests\DebugForgeStudio.Tests.csproj

if ($LASTEXITCODE -ne 0) {
    throw 'DebugForge Studio deterministic tests failed.'
}

if (-not $SkipEvidenceCapture) {
    & (Join-Path $RepoRoot 'tools\capture-native-evidence.ps1')
}

$Python=Resolve-Python
$Args=@((Join-Path $RepoRoot 'tools\verify_release.py'))
if ($AllowPendingReview) { $Args += '--allow-pending-review' }

& $Python @Args
$VerifyExit=$LASTEXITCODE

if ($VerifyExit -ne 0) {
    throw "Static repository verification failed with exit code $VerifyExit."
}

Write-Host 'DEBUGFORGE STUDIO RELEASE VERIFICATION PASSED' -ForegroundColor Green
