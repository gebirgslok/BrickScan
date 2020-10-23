[CmdletBinding()]
param(
	[Parameter(Mandatory=$True)]
	[string]$PackagesDirectory,
    [Parameter(Mandatory=$True)]
    [string]$VariableName
)

Write-Host "Revolving Squirrel tools directory from $PackagesDirectory"
Write-Host "The specified packages directory contains the following sub dirs:"
Get-ChildItem $PackagesDirectory -Directory
Write-Host

[System.IO.DirectoryInfo]$DirectoryInfo = Get-ChildItem $PackagesDirectory | Where-Object { $_.PSIsContainer -and $_.Name.StartsWith("squirrel.windows")} | Select -First 1
[string]$PackageBaseDirectory = $DirectoryInfo.Fullname;

Write-Host "Found squirrel.windows base package directory = $PackageBaseDirectory"

[System.IO.DirectoryInfo]$VersionDirectoryInfo = Get-ChildItem $PackageBaseDirectory -Directory | Sort-Object -Property Name | Select -First 1
[string]$FullDirectory = $VersionDirectoryInfo.Fullname;

Write-Host "Full path to squirrel.windows package = $FullDirectory"

[string]$ToolsPath = Join-Path $FullDirectory -ChildPath "tools"

Write-Host "Full path to Squirrel tooling = $ToolsPath"

Write-Host "Writing $ToolsPath to $VariableName"
Write-Host "##vso[task.setvariable variable=$VariableName;]$ToolsPath"