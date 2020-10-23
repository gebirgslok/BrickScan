[CmdletBinding()]
param(
	[Parameter(Mandatory=$True)]
	[string]$ReleaseFolder,
	[Parameter(Mandatory=$True)]
	[string]$ConnectionString,
	[Parameter(Mandatory=$True)]
	[string]$Container
)

Write-Host "Connecting to Azure Blob storage"kk
Write-Host "Connection string = " $ConnectionString
$storage_account = New-AzStorageContext -ConnectionString $ConnectionString

Get-ChildItem -Path $ReleaseFolder -File -Recurse | Set-AzStorageBlobContent -Container $Container -Force -Context $storage_account