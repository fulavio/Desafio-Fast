#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
& node (Join-Path $PSScriptRoot 'run-project.mjs') MySql
exit $LASTEXITCODE
