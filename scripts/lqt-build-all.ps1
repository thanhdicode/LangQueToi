# Làng Quê Tôi — master orchestration script.
# Runs the full Phase 1 → 2 pipeline in Unity batch mode + produces a Windows exe.
# REQUIREMENT: Unity Editor must be closed (no UnityLockfile in Temp/).
#
# Steps:
#   1. Data localizer      (dry-run then apply)
#   2. Static text         (dry-run then apply)
#   3. Font builder        (Nunito → TMP SDF)
#   4. Art importer        (spec settings + dimension validation)
#   5. Visual scene setup  (sprite bindings + font swap + DialoguePanel wiring)
#   6. Localization validator gate
#   7. Windows IL2CPP standalone build
#
# Every step logs to Artifacts/Logs/ and fails fast on non-zero Unity exit code.

param(
    [string]$UnityExe = 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe',
    [string]$Project  = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'

$LockFile = Join-Path $Project 'Temp\UnityLockfile'
if (Test-Path $LockFile) {
    Write-Host "ERROR: UnityLockfile exists at $LockFile" -ForegroundColor Red
    Write-Host "Close Unity Editor before running this script." -ForegroundColor Red
    exit 2
}

if (-not (Test-Path $UnityExe)) {
    Write-Host "ERROR: Unity not found at $UnityExe" -ForegroundColor Red
    exit 3
}

$LogsDir = Join-Path $Project 'Artifacts\Logs'
New-Item -ItemType Directory -Force $LogsDir | Out-Null

function Invoke-UnityMethod {
    param(
        [Parameter(Mandatory)][string]$Method,
        [Parameter(Mandatory)][string]$LogName
    )
    $log = Join-Path $LogsDir "$LogName.log"
    Write-Host ""
    Write-Host "==> $Method" -ForegroundColor Cyan
    Write-Host "    log: $log"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & $UnityExe -batchmode -quit -nographics `
        -projectPath $Project `
        -executeMethod $Method `
        -logFile $log
    $code = $LASTEXITCODE
    $sw.Stop()
    Write-Host ("    exit={0}  duration={1:mm\:ss}" -f $code, $sw.Elapsed)
    if ($code -ne 0) {
        Write-Host "FAILED: $Method (exit $code). See $log" -ForegroundColor Red
        exit $code
    }
}

Write-Host "Làng Quê Tôi — full pipeline" -ForegroundColor Yellow
Write-Host "Project: $Project"
Write-Host "Unity:   $UnityExe"

Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTDataLocalizer.DryRunFromCommandLine'    -LogName '01-data-dry-run'
Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTDataLocalizer.ApplyFromCommandLine'     -LogName '02-data-apply'
Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTStaticTextLocalizer.DryRunFromCommandLine' -LogName '03-static-dry-run'
Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTStaticTextLocalizer.ApplyFromCommandLine'  -LogName '04-static-apply'
Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTFontBuilder.BuildFromCommandLine'       -LogName '05-font-build'
Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTArtImporter.ConfigureAndValidateFromCommandLine' -LogName '06-art-import'
Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTVisualSceneSetup.ApplyFromCommandLine'  -LogName '07-scene-setup'
Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTLocalizationValidator.RunFromCommandLine' -LogName '08-loc-validator'
Invoke-UnityMethod -Method 'LangQueToi.EditorTools.LQTBuild.BuildFromCommandLine'             -LogName '09-windows-build'

Write-Host ""
Write-Host "DONE." -ForegroundColor Green
$exe = Join-Path $Project 'Builds\LangQueToi\LangQueToi.exe'
if (Test-Path $exe) {
    Write-Host "Executable: $exe"
    $size = (Get-Item $exe).Length
    Write-Host ("Size: {0:N0} bytes" -f $size)
} else {
    Write-Host "WARNING: Expected executable not found at $exe" -ForegroundColor Yellow
}
