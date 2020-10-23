[CmdletBinding()]
param(
	[Parameter(Mandatory=$True)]
	[string]$ReleaseFolder,
	[Parameter(Mandatory=$True)]
	[string]$ConnectionString,
	[Parameter(Mandatory=$True)]
	[string]$Container
)

Write-Host "Connecting to Azure Blob storage"
Write-Host "Connection string = $ConnectionString"
$StorageAccount = New-AzStorageContext -ConnectionString $ConnectionString

Write-Host "Downloading existing releases to temporary release directory = $ReleaseFolder" 
$Blobs = Get-AzStorageBlob -Container $Container -Context $StorageAccount

New-Item -ItemType Directory -Force -Path $ReleaseFolder 
foreach ($Blob in $Blobs)
{
    Write-Host "Downloading " $Blob.Name
	Get-AzStorageBlobContent `
        -Container $Container -Blob $Blob.Name -Force -Destination $ReleaseFolder `
        -Context $StorageAccount
}


