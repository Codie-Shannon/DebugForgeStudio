$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

function Resolve-Python {
    foreach ($Name in @(
        'python.exe',
        'python3.exe',
        'python',
        'python3'
    )) {
        $Command = Get-Command $Name -ErrorAction SilentlyContinue

        if (
            $Command -and
            $Command.Source -and
            $Command.Source -notmatch '(?i)\\WindowsApps\\'
        ) {
            return $Command.Source
        }
    }

    $Py = Get-Command py.exe -ErrorAction SilentlyContinue

    if ($Py) {
        $Output = @(
            & $Py.Source -3 -c 'import sys; print(sys.executable)' 2>&1
        )

        if ($LASTEXITCODE -eq 0) {
            $Candidate = $Output |
                ForEach-Object { "$_".Trim() } |
                Where-Object {
                    $_ -and (
                        Test-Path `
                            -LiteralPath $_ `
                            -PathType Leaf
                    )
                } |
                Select-Object -First 1

            if ($Candidate) {
                return $Candidate
            }
        }
    }

    throw 'Python 3.11 or later is required for repository verification.'
}

$Python = Resolve-Python

& $Python .\tools\verify_release.py
if ($LASTEXITCODE -ne 0) {
    throw 'Static repository verification failed.'
}

Write-Host 'DEBUGFORGE STUDIO GENERATED BASELINE VERIFIED' -ForegroundColor Green
