#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = Split-Path -Parent $PSScriptRoot
$backendSolution = Join-Path $projectRoot 'backend/Fast.Workshops.sln'
$frontendRoot = Join-Path $projectRoot 'frontend'
$frontendPackage = Join-Path $frontendRoot 'package.json'

foreach ($requiredCommand in @('dotnet', 'npm.cmd')) {
    if (-not (Get-Command $requiredCommand -ErrorAction SilentlyContinue)) {
        throw "Comando '$requiredCommand' ausente; esperado um executavel no PATH."
    }
}

foreach ($requiredFile in @($backendSolution, $frontendPackage)) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Arquivo '$requiredFile' ausente; esperado um arquivo de projeto existente."
    }
}

& dotnet restore $backendSolution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& npm.cmd --prefix $frontendRoot ci
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Setup concluido. Execute .\scripts\check.ps1.'
