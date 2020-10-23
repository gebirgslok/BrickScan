[CmdletBinding()]
param(
	[Parameter(Mandatory=$True)]
	[string]$NuspecFile,
    [Parameter(Mandatory=$True)]
    [string]$VariableName
)

Write-Host "Reading Version from nuspec file =" $NuspecFile

[xml]$nuspecXml = Get-Content -Path $NuspecFile

. ".\helpers.ps1"

Write-Host
Write-Host "NUSPEC Content:"
Write-Xml -xml $nuspecXml
Write-Host

$version = $nuspecXml.package.metadata.version
Write-Host "Read version =" $version

Write-Host "Writing $version to $VariableName"
Write-Host "##vso[task.setvariable variable=$VariableName;]$version"