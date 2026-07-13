[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$webProject = Join-Path $repositoryRoot "src\SuperDentist.Web"
$node = Get-Command "node" -ErrorAction SilentlyContinue
$isWindowsPlatform = $env:OS -eq "Windows_NT"
$npmName = if ($isWindowsPlatform) { "npm.cmd" } else { "npm" }
$npm = Get-Command $npmName -ErrorAction SilentlyContinue

if ($null -eq $node) {
    throw "Required tool 'node' was not found on PATH."
}

if ($null -eq $npm) {
    throw "Required tool '$npmName' was not found on PATH."
}

function Invoke-Npm {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & $npm.Source @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "npm failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

Push-Location $webProject
try {
    Invoke-Npm @("ci")
    Invoke-Npm @("run", "typecheck")
    Invoke-Npm @("run", "lint")
    Invoke-Npm @("test")
    Invoke-Npm @("run", "build")
}
finally {
    Pop-Location
}
