[CmdletBinding()]
param(
	[Parameter(Mandatory=$True)]
	[string]$SrcDirectory,
	[Parameter(Mandatory=$True)]
	[int]$BuildNumber,
	[Parameter(Mandatory=$True)]
	[string]$InformationalVersionSuffix
)

. "$PSScriptRoot\helpers.ps1"

Function Update-CsProj-Versions([string]$CsProjPath, [int]$BuildNumber, [string]$InformationalVersionSuffix)
{
    Write-Host "Patching .csproj file = $CsProjPath"
    Write-Host "Build number = $BuildNumber"
    Write-Host "Informational version suffix = $InformationalVersionSuffix"

    [xml]$CsProjContent = Get-Content -Path $CsProjPath -Encoding UTF8

    Write-Host
    Write-Host ".csproj Content:"
    Write-Xml -xml $CsProjContent
    Write-Host

    $FileVersionNode = $CsProjContent.SelectSingleNode("//Project/PropertyGroup/FileVersion")
    [string]$FileVersion = $FileVersionNode.InnerText

    Write-Host "Read FileVersion = $FileVersion"

    $Split = $FileVersion.Split(".")

    Write-Host "Major = $($Split[0]), Minor = $($Split[1]), Patch = $($Split[2])"

    [string]$UpdatedFileVersion = "$($Split[0]).$($Split[1]).$($Split[2]).$BuildNumber"
    $FileVersionNode.InnerText = $UpdatedFileVersion

    Write-Host "Set updated FileVersion = $UpdatedFileVersion"

    $UpdatedVersion = "$($Split[0]).0.0.0"

    Write-Host "Set updated Version = $UpdatedVersion"

    $VersionNode = $CsProjContent.SelectSingleNode("//Project/PropertyGroup/Version")
    $VersionNode.InnerText = $UpdatedVersion

    [string]$UpdatedInformationalVersion = "$($Split[0]).$($Split[1]).$($Split[2])-$InformationalVersionSuffix"

    Write-Host "Set updated InformationalVersion = $UpdatedInformationalVersion"

    $InformationalVersionNode = $CsProjContent.SelectSingleNode("//Project/PropertyGroup/InformationalVersion")
    [string]$InformationalVersionNode.InnerText = $UpdatedInformationalVersion

    Write-Host
    Write-Host "Udated .csproj Content:"
    Write-Xml -xml $CsProjContent
    Write-Host

    Write-Host "Saving updated .csproj file to $CsProjPath"
    $CsProjContent.Save($CsProjPath)
}

$CsProjPaths = Get-ChildItem -Path $SrcDirectory -Filter "*.csproj" -Recurse | % { $_.FullName }

foreach ($CsProjPath in $CsProjPaths)
{   
    Update-CsProj-Versions -CsProjPath $CsProjPath -BuildNumber $BuildNumber -InformationalVersionSuffix $InformationalVersionSuffix
    Write-Host
}
