[CmdletBinding()]
param(
    [string] $ConfigPath = "$PSScriptRoot\project.build.json",
    [switch] $UpdateVersions,
    [switch] $Build,
    [switch] $PublishNuget = $false,
    [switch] $PublishGitHub = $false,
    [switch] $Plan,
    [string] $PlanPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

try {
    Import-Module PSPublishModule -Force -ErrorAction Stop
} catch {
    throw "PSPublishModule is required for Build-Project.ps1. Install it with 'Install-Module PSPublishModule -Scope CurrentUser' and retry. $($_.Exception.Message)"
}

$invokeParams = @{
    ConfigPath = $ConfigPath
}
if ($PSBoundParameters.ContainsKey('UpdateVersions')) { $invokeParams.UpdateVersions = $UpdateVersions.IsPresent }
if ($PSBoundParameters.ContainsKey('Build')) { $invokeParams.Build = $Build.IsPresent }
if ($PSBoundParameters.ContainsKey('PublishNuget')) { $invokeParams.PublishNuget = $PublishNuget.IsPresent }
if ($PSBoundParameters.ContainsKey('PublishGitHub')) { $invokeParams.PublishGitHub = $PublishGitHub.IsPresent }
if ($PSBoundParameters.ContainsKey('Plan')) { $invokeParams.Plan = $Plan.IsPresent }
if ($PlanPath) { $invokeParams.PlanPath = $PlanPath }

Invoke-ProjectBuild @invokeParams
