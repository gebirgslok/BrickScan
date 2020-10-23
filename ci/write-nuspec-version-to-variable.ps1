[CmdletBinding()]
param(
	[Parameter(Mandatory=$True)]
	[string]$NuspecFile,
    [Parameter(Mandatory=$True)]
    [string]$VariableName
)

Function Write-Xml([xml]$xml)
{
    $StringWriter = New-Object System.IO.StringWriter;
    $XmlWriter = New-Object System.Xml.XmlTextWriter $StringWriter;
    $XmlWriter.Formatting = "indented";
    $xml.WriteTo($XmlWriter);
    $XmlWriter.Flush();
    $StringWriter.Flush();
    Write-Host $StringWriter.ToString();
}

Write-Host "Reading Version from nuspec file =" $NuspecFile

[xml]$nuspecXml = Get-Content -Path $NuspecFile

Write-Host
Write-Host "NUSPEC Content:"
Write-Xml -xml $nuspecXml
Write-Host

$version = $nuspecXml.package.metadata.version
Write-Host "Read version =" $version

Write-Host "Writing $version to $VariableName"
Write-Host "##vso[task.setvariable variable=$VariableName;]$version"