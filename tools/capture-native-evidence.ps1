param([string]$RepoRoot=(Split-Path -Parent $PSScriptRoot))

Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Set-Location $RepoRoot

function Resolve-Browser {
    foreach ($Path in @(
        'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe',
        'C:\Program Files\Microsoft\Edge\Application\msedge.exe',
        'C:\Program Files\Google\Chrome\Application\chrome.exe',
        'C:\Program Files (x86)\Google\Chrome\Application\chrome.exe'
    )) {
        if (Test-Path -LiteralPath $Path -PathType Leaf) { return $Path }
    }
    throw 'Microsoft Edge or Google Chrome is required.'
}

$Browser=Resolve-Browser
$Dll=Join-Path $RepoRoot 'src\DebugForgeStudio.Web\bin\Release\net8.0\DebugForgeStudio.Web.dll'
$LogRoot=Join-Path $RepoRoot 'artifacts\native-web'
$Out=Join-Path $LogRoot 'stdout.log'
$Err=Join-Path $LogRoot 'stderr.log'
$BaseUrl='http://127.0.0.1:5196'

New-Item -ItemType Directory -Force -Path $LogRoot | Out-Null
Remove-Item -LiteralPath $Out,$Err -Force -ErrorAction SilentlyContinue

dotnet build .\src\DebugForgeStudio.Web\DebugForgeStudio.Web.csproj --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'DebugForge web build failed.' }

$Process=Start-Process `
    -FilePath 'dotnet' `
    -ArgumentList @($Dll,'--urls',$BaseUrl) `
    -WorkingDirectory (Split-Path $Dll -Parent) `
    -RedirectStandardOutput $Out `
    -RedirectStandardError $Err `
    -PassThru

try {
    $Healthy=$false
    for ($i=1;$i -le 40;$i++) {
        Start-Sleep -Milliseconds 500
        try {
            $h=Invoke-RestMethod -Uri "$BaseUrl/health" -TimeoutSec 2
            if ($h.status -eq 'Healthy') { $Healthy=$true; break }
        } catch {}
        if ($Process.HasExited) { break }
    }

    if (-not $Healthy) {
        throw 'DebugForge web host did not become healthy.'
    }

    $Lines=@(
        '2026-08-11T08:00:00Z INFO start',
        '2026-08-11T08:00:01Z WARN retry 1',
        '2026-08-11T08:00:02Z ERROR timeout request 123',
        '2026-08-11T08:00:03Z ERROR timeout request 456'
    )

    $ScanBody=@{lines=$Lines;contextRadius=1}|ConvertTo-Json -Depth 5
    $Scan=Invoke-RestMethod -Uri "$BaseUrl/api/scan" -Method Post -ContentType 'application/json' -Body $ScanBody
    $Triage=Invoke-RestMethod -Uri "$BaseUrl/api/triage" -Method Post -ContentType 'application/json' -Body $ScanBody

    $ReproBody=@{actions=@('Open export','Run import')}|ConvertTo-Json
    $Repro=Invoke-RestMethod -Uri "$BaseUrl/api/reproduction" -Method Post -ContentType 'application/json' -Body $ReproBody

    $HypBody=@{id='H1';description='Delimiter mismatch';evidence=@('header differs')}|ConvertTo-Json
    $Hyp=Invoke-RestMethod -Uri "$BaseUrl/api/hypothesis" -Method Post -ContentType 'application/json' -Body $HypBody

    $CompareBody=@{working=@('id,status','1,Ready');broken=@('id;status','1,Ready','2,Failed')}|ConvertTo-Json
    $Compare=Invoke-RestMethod -Uri "$BaseUrl/api/compare" -Method Post -ContentType 'application/json' -Body $CompareBody

    if (
        @($Scan.findings).Count -ne 3 -or
        $Triage.state -ne 'Investigate' -or
        @($Repro).Count -ne 2 -or
        $Hyp.state -ne 'Candidate' -or
        @($Compare).Count -ne 2
    ) {
        throw 'DebugForge native API smoke failed.'
    }

    $Specs=@(
        [pscustomobject]@{Group='screenshot-group-01-log-intake-streaming-scan-and-incident-triage';File='01-desktop-overview.png';Page='overview.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-01-log-intake-streaming-scan-and-incident-triage';File='02-desktop-workflow.png';Page='workflow.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-01-log-intake-streaming-scan-and-incident-triage';File='03-desktop-review.png';Page='review.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-01-log-intake-streaming-scan-and-incident-triage';File='04-mobile-workflow.png';Page='workflow.html';Size='390,844'}
        [pscustomobject]@{Group='screenshot-group-02-reproduction-hypotheses-and-file-comparison';File='01-desktop-assurance.png';Page='assurance.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-02-reproduction-hypotheses-and-file-comparison';File='02-desktop-reporting.png';Page='reporting.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-02-reproduction-hypotheses-and-file-comparison';File='03-desktop-boundaries.png';Page='boundaries.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-02-reproduction-hypotheses-and-file-comparison';File='04-mobile-assurance.png';Page='assurance.html';Size='390,844'}
        [pscustomobject]@{Group='screenshot-group-03-reports-evidence-export-and-product-boundaries';File='01-desktop-operations.png';Page='operations.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-03-reports-evidence-export-and-product-boundaries';File='02-desktop-evidence.png';Page='evidence.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-03-reports-evidence-export-and-product-boundaries';File='03-desktop-release.png';Page='release.html';Size='1440,900'}
        [pscustomobject]@{Group='screenshot-group-03-reports-evidence-export-and-product-boundaries';File='04-mobile-evidence.png';Page='evidence.html';Size='390,844'}
    )

    $Root=Join-Path $RepoRoot 'docs\screenshot-groups'
    $Profile=Join-Path $env:TEMP ('DebugForge-Browser-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Force -Path $Profile|Out-Null

    try {
        foreach ($Spec in $Specs) {
            $Dir=Join-Path $Root $Spec.Group
            New-Item -ItemType Directory -Force -Path $Dir|Out-Null
            $Output=Join-Path $Dir $Spec.File
            Remove-Item -LiteralPath $Output -Force -ErrorAction SilentlyContinue

            $Args=@(
                '--headless=new','--disable-gpu','--hide-scrollbars',
                "--user-data-dir=$Profile",
                "--window-size=$($Spec.Size)",
                "--screenshot=$Output",
                "$BaseUrl/$($Spec.Page)"
            )

            $Cap=Start-Process -FilePath $Browser -ArgumentList $Args -Wait -PassThru
            if ($Cap.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $Output)) {
                throw "Screenshot failed: $($Spec.File)"
            }
        }
    }
    finally {
        Remove-Item -LiteralPath $Profile -Recurse -Force -ErrorAction SilentlyContinue
    }

    $Smoke=[ordered]@{
        health=$h
        scan=$Scan
        triage=$Triage
        reproduction=$Repro
        hypothesis=$Hyp
        comparison=$Compare
    }

    [System.IO.File]::WriteAllText(
        (Join-Path $LogRoot 'api-smoke.json'),
        (($Smoke|ConvertTo-Json -Depth 20)+"`n"),
        (New-Object System.Text.UTF8Encoding($false))
    )

    Write-Host 'DEBUGFORGE STUDIO NATIVE WEB EVIDENCE PASSED' -ForegroundColor Green
    Write-Host 'Scan/triage/reproduction/hypothesis/comparison smoke: passed'
    Write-Host 'Native screenshots: 12'
}
finally {
    if ($Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
        $Process.WaitForExit()
    }
}
