[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "verify-backend.ps1")
& (Join-Path $PSScriptRoot "verify-frontend.ps1")
