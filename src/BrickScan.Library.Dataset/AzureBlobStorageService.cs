#region License
// Copyright (c) 2020 Jens Eisenbach
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using BrickScan.Library.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrickScan.Library.Dataset
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobContainerClient _blobContainerClient;
        private readonly ILogger<AzureBlobStorageService> _logger;

        public AzureBlobStorageService(IConfiguration configuration, ILogger<AzureBlobStorageService> logger)
        {
            _logger = logger;
            var connectionString = configuration.GetConnectionString("AzureStorageConnectionString");
            var blobServiceClient = new BlobServiceClient(connectionString);
            _blobContainerClient = blobServiceClient.GetBlobContainerClient(configuration["AzureStorage:DatasetImagesContainerName"]);
        }

        private async Task UploadAsync(BlobClient blobClient, ImageData imageData)
        {
            using var uploadFileStream = new MemoryStream(imageData.RawBytes, writable: false);

            var response = await blobClient.UploadAsync(uploadFileStream, true);

            using var rawResponse = response.GetRawResponse();

            if (rawResponse.Status != 201)
            {
                _logger.LogError("Failed to upload image = ({BlobName}, {Uri}). Received status code = {StatusCode} ({ReasonPhrase}).",
                    blobClient.Name, blobClient.Uri.ToString(), rawResponse.Status, rawResponse.ReasonPhrase);

                throw new AzureStorageUploadBlobException(blobClient.Uri, rawResponse.Status, rawResponse.ReasonPhrase);
            }

            _logger.LogInformation("Successfully uploaded image = {BlobName} to {Uri}.", blobClient.Name, blobClient.Uri);

            uploadFileStream.Close();
        }

        public async Task DeleteImageAsync(Uri imageUri)
        {
            string blobName = Path.GetFileName(imageUri.LocalPath);
            var response = await _blobContainerClient.DeleteBlobIfExistsAsync(blobName);

            if (!response.Value)
            {
                using var rawResponse = response.GetRawResponse();

                if (rawResponse.Status != 202)
                {
                    _logger.LogError("Failed to delete image = ({BlobName}, {Uri}). Received status code = {StatusCode} ({ReasonPhrase}).",
                        blobName, imageUri.ToString(), rawResponse.Status, rawResponse.ReasonPhrase);

                    throw new AzureStorageDeleteBlobException(blobName, rawResponse.Status, rawResponse.ReasonPhrase);
                }
            }

            _logger.LogInformation("Successfully deleted image blob {BlobName}.", blobName);
        }

        public async Task<Uri> StoreImageAsync(ImageData imageData)
        {
            var filename = StorageServiceHelper.GenerateImageFilename(imageData.Format.ToFileExtension());
            var blobClient = _blobContainerClient.GetBlobClient(filename);
            await UploadAsync(blobClient, imageData);
            return blobClient.Uri;
        }

        public async Task<List<Uri>> StoreImagesAsync(List<ImageData> imageDataList)
        {
            var blobsClientData = imageDataList.Select(x =>
            {
                var filename = StorageServiceHelper.GenerateImageFilename(x.Format.ToFileExtension());
                return new
                {
                    blobClient = _blobContainerClient.GetBlobClient(filename),
                    imageData = x

                };
            }).ToList();

            var uploadTasks = blobsClientData.Select(x => UploadAsync(x.blobClient, x.imageData));

            await Task.WhenAll(uploadTasks);

            return blobsClientData.Select(x => x.blobClient.Uri).ToList();
        }
    }
}