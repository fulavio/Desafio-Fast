#Requires -Version 5.1
$ErrorActionPreference = 'Stop'
& node (Join-Path $PSScriptRoot 'seed-mysql.mjs')
exit $LASTEXITCODE
