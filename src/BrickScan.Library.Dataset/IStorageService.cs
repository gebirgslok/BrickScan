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
using System.Threading.Tasks;
using BrickScan.Library.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrickScan.Library.Dataset
{
    public class LocalFileStorageService : IStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LocalFileStorageService> _logger;
        
        public LocalFileStorageService(IConfiguration configuration, 
            ILogger<LocalFileStorageService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<Uri> StoreImageAsync(ImageData imageData)
        {
            var filenameWithoutExtension = StorageServiceHelper.GenerateImageFilename();
            var ext = imageData.Format.ToFileExtension();

            _logger.LogDebug("Generated new filename {Filename}, received extension {Extension}.", 
                filenameWithoutExtension, ext);

            var directory = _configuration.GetValue<string>("StorageService:Directory");
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, Path.ChangeExtension(filenameWithoutExtension, ext));

            _logger.LogDebug("Writing image ({RawBytes}b, format = {Format}) to file {FilePath}...", 
                imageData.RawBytes.Length, imageData.Format, filePath);

            File.WriteAllBytes(filePath, imageData.RawBytes);

            _logger.LogDebug("Successfully wrote {RawBytes}b to file {FilePath}.", imageData.RawBytes.Length, filePath);

            return Task.FromResult(new Uri(filePath));
        }

        public async Task<List<Uri>> StoreImagesAsync(List<ImageData> imageDataList)
        {
            var uris = new List<Uri>();

            foreach (var imageData in imageDataList)
            {
                var uri = await StoreImageAsync(imageData);
                uris.Add(uri);
            }

            return uris;
        }
    }

    public class AzureBlobStorageService : IStorageService
    {
        public Task<Uri> StoreImageAsync(ImageData imageData)
        {
            throw new NotImplementedException();
        }

        public Task<List<Uri>> StoreImagesAsync(List<ImageData> imageDataList)
        {
            throw new NotImplementedException();
        }
    }

    public interface IStorageService
    {
        Task<Uri> StoreImageAsync(ImageData imageDatá);

        Task<List<Uri>> StoreImagesAsync(List<ImageData> imageDataList);
    }
}
