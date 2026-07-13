[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
if ($null -eq $dotnet) {
    throw "Required tool 'dotnet' was not found on PATH."
}

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & $dotnet.Source @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

Push-Location $repositoryRoot
try {
    Invoke-DotNet @("restore", "Super Dentist.sln")
    Invoke-DotNet @(
        "build",
        "Super Dentist.sln",
        "-c", "Release",
        "--no-restore",
        "-warnaserror"
    )
    Invoke-DotNet @(
        "test",
        "Super Dentist.sln",
        "-c", "Release",
        "--no-build",
        "--no-restore"
    )
}
finally {
    Pop-Location
}
