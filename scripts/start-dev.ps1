[CmdletBinding()]
param(
    [string]$ApiUrl = "http://localhost:5080",
    [string]$WebUrl = "http://localhost:5173"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$apiProject = Join-Path $repositoryRoot "src\SuperDentist.Api"
$webProject = Join-Path $repositoryRoot "src\SuperDentist.Web"
$apiAssembly = "bin\Debug\net8.0\SuperDentist.Api.dll"
$viteEntryPoint = Join-Path $webProject "node_modules\vite\bin\vite.js"
$isWindowsPlatform = $env:OS -eq "Windows_NT"
$previousAspNetCoreEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousDotNetEnvironment = $env:DOTNET_ENVIRONMENT
$previousViteApiBaseUrl = $env:VITE_API_BASE_URL
$apiProcess = $null
$webProcess = $null

function Get-RequiredCommand {
    param([Parameter(Mandatory)][string]$Name)

    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "Required tool '$Name' was not found on PATH."
    }

    return $command.Source
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)][string]$Command,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command $($Arguments -join ' ')"
    }
}

function Wait-ForEndpoint {
    param(
        [Parameter(Mandatory)][string]$Url,
        [Parameter(Mandatory)][System.Diagnostics.Process]$Process,
        [Parameter(Mandatory)][string]$Name
    )

    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($Process.HasExited) {
            throw "$Name exited before becoming ready (exit code $($Process.ExitCode))."
        }

        try {
            $null = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "$Name did not become ready at $Url within 30 seconds."
}

function Stop-OwnedProcess {
    param([AllowNull()][System.Diagnostics.Process]$Process)

    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -ErrorAction SilentlyContinue
        $Process.WaitForExit(5000) | Out-Null
    }
}

try {
    $dotnet = Get-RequiredCommand "dotnet"
    $node = Get-RequiredCommand "node"
    $npmName = if ($isWindowsPlatform) { "npm.cmd" } else { "npm" }
    $null = Get-RequiredCommand $npmName

    if (-not (Test-Path -LiteralPath $viteEntryPoint -PathType Leaf)) {
        throw "Frontend dependencies are missing. Run 'npm ci' in src/SuperDentist.Web first."
    }

    $webUri = [Uri]$WebUrl
    Write-Host "Building the API..."
    Push-Location $repositoryRoot
    try {
        Invoke-CheckedCommand $dotnet @(
            "build",
            "src/SuperDentist.Api/SuperDentist.Api.csproj",
            "--nologo"
        )
    }
    finally {
        Pop-Location
    }

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:DOTNET_ENVIRONMENT = "Development"
    if ([string]::IsNullOrWhiteSpace($env:VITE_API_BASE_URL)) {
        $env:VITE_API_BASE_URL = $ApiUrl
    }
    $apiProcess = Start-Process `
        -FilePath $dotnet `
        -ArgumentList @($apiAssembly, "--urls", $ApiUrl) `
        -WorkingDirectory $apiProject `
        -NoNewWindow `
        -PassThru

    $webProcess = Start-Process `
        -FilePath $node `
        -ArgumentList @(
            "node_modules/vite/bin/vite.js",
            "--host", $webUri.Host,
            "--port", $webUri.Port.ToString(),
            "--strictPort"
        ) `
        -WorkingDirectory $webProject `
        -NoNewWindow `
        -PassThru

    Wait-ForEndpoint "$ApiUrl/health" $apiProcess "API"
    Wait-ForEndpoint $WebUrl $webProcess "React development server"

    Write-Host ""
    Write-Host "Super Dentist development services are ready:"
    Write-Host "  API:     $ApiUrl"
    Write-Host "  Swagger: $ApiUrl/swagger"
    Write-Host "  Web:     $WebUrl"
    Write-Host ""
    Write-Host "Press Ctrl+C to stop both services."

    while ($true) {
        if ($apiProcess.HasExited) {
            throw "API exited unexpectedly with code $($apiProcess.ExitCode)."
        }

        if ($webProcess.HasExited) {
            throw "React development server exited unexpectedly with code $($webProcess.ExitCode)."
        }

        Start-Sleep -Seconds 1
    }
}
finally {
    Stop-OwnedProcess $webProcess
    Stop-OwnedProcess $apiProcess

    if ($null -eq $previousAspNetCoreEnvironment) {
        Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:ASPNETCORE_ENVIRONMENT = $previousAspNetCoreEnvironment
    }

    if ($null -eq $previousDotNetEnvironment) {
        Remove-Item Env:DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_ENVIRONMENT = $previousDotNetEnvironment
    }

    if ($null -eq $previousViteApiBaseUrl) {
        Remove-Item Env:VITE_API_BASE_URL -ErrorAction SilentlyContinue
    }
    else {
        $env:VITE_API_BASE_URL = $previousViteApiBaseUrl
    }
}
