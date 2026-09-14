#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = Split-Path -Parent $PSScriptRoot
$backendSolution = Join-Path $projectRoot 'backend/Fast.Workshops.sln'
$frontendRoot = Join-Path $projectRoot 'frontend'

& dotnet format $backendSolution --verify-no-changes --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& dotnet build $backendSolution --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& dotnet test $backendSolution --no-restore --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& npm.cmd --prefix $frontendRoot run format:check
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& npm.cmd --prefix $frontendRoot run lint
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& npm.cmd --prefix $frontendRoot run build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& npm.cmd --prefix $frontendRoot test -- --watch=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Todas as verificacoes passaram.'
