param(
    [ValidateSet('Debug','Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
    dotnet restore .\ChartForgeX.sln
    dotnet build .\ChartForgeX.sln -c $Configuration --no-restore
    dotnet run --project .\ChartForgeX.Examples\ChartForgeX.Examples.csproj -c $Configuration
} finally {
    Pop-Location
}
