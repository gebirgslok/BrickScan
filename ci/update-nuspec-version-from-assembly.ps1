[CmdletBinding()]
param(
	[Parameter(Mandatory=$True)]
	[string]$SourceAssembly,
    [Parameter(Mandatory=$True)]
    [string]$DestinationNuspec
)

. ".\helpers.ps1"

Write-Host "Reading version from source assembly = $SourceAssembly"
Write-Host "Writing retrieved version to Nuspec file = $DestinationNuspec"

[string]$Version = Read-Assembly-Version -PathToAssembly $SourceAssembly

Write-Host "Read assembly version = $Version"

[xml]$NuspecXml = Get-Content -Path $DestinationNuspec -Encoding UTF8

Write-Host
Write-Host "NUSPEC Content:"
Write-Xml -xml $NuspecXml
Write-Host

Write-Host "Updating Nuspec file version to $Version" 

$Node = $NuspecXml.SelectSingleNode("//package/metadata/version")
$Node.InnerText = $Version

Write-Host
Write-Host "Updated NUSPEC content:"
Write-Xml -xml $NuspecXml
Write-Host

Write-Host "Saving updated Nuspec file to $DestinationNuspec"
$NuspecXml.Save($DestinationNuspec)