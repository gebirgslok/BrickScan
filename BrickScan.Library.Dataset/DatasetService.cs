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
using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Core;
using BrickScan.Library.Dataset.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrickScan.Library.Dataset
{
    public class DatasetService : IDatasetService
    {
        private readonly IStorageService _storageService;
        private readonly DatasetDbContext _datasetDbContext;
        private readonly ILogger<DatasetService> _logger;

        public DatasetService(IStorageService storageService,
            DatasetDbContext datasetDbContext,
            ILogger<DatasetService> logger)
        {
            _storageService = storageService;
            _datasetDbContext = datasetDbContext;
            _logger = logger;
        }

        public async Task<DatasetImage> AddUnclassifiedImageAsync(ImageData imageData)
        {
            _logger.LogDebug("Adding unclassified image data ({ByteCount} bytes, format: {ImageFormat}).",
                    imageData.RawBytes.Length, imageData.Format);

            var uri = await _storageService.StoreImageAsync(imageData);
            var datasetImage = new DatasetImage
            {
                CreatedOn = DateTime.UtcNow,
                Status = EntityStatus.Unclassified,
                Url = uri.AbsoluteUri
            };

            _logger.LogDebug("Adding unclassified datasetImage {@DatasetImage} into database.", datasetImage);

            var entry = await _datasetDbContext.DatasetImages.AddAsync(datasetImage);
            await _datasetDbContext.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<List<DatasetImage>> AddUnclassifiedImagesAsync(List<ImageData> imageDataList)
        {
            _logger.LogDebug("Adding {NumOfImages} unclassified images (total {TotalByteCount} bytes, contained formats: {ImageFormats}).",
                imageDataList.Count,
                imageDataList.GetTotalByteCount(),
                imageDataList.GetIncludedFormatsString());

            var utcNow = DateTime.UtcNow;
            var uris = await _storageService.StoreImagesAsync(imageDataList);

            var datasetImages = uris.Select(uri => new DatasetImage
            {
                CreatedOn = utcNow,
                Status = EntityStatus.Unclassified,
                Url = uri.AbsoluteUri
            })
                .ToList();

            _logger.LogDebug("Created dataset images {@DatasetImages}. Inserting them into database).", datasetImages);

            await _datasetDbContext.AddRangeAsync(datasetImages);
            _logger.LogDebug($"Calling {nameof(_datasetDbContext.SaveChangesAsync)} on {nameof(_datasetDbContext)}.");

            var numOfAffectedRows = await _datasetDbContext.SaveChangesAsync();

            _logger.LogInformation("Received #{NumOfAffectedRows} affected rows after adding and saving {NumOfDatasetImages}.",
                numOfAffectedRows, datasetImages.Count);
            return datasetImages;
        }

        public async Task<DatasetImage?> FindImageByIdAsync(int imageId)
        {
            _logger.LogDebug("Retrieving image for {ImageId}.", imageId);
            var datasetImage = await _datasetDbContext.DatasetImages.FirstOrDefaultAsync(image => image.Id == imageId);
            _logger.LogDebug("Returned {@Image} for {ImageId}", datasetImage, imageId);
            return datasetImage;
        }

        public async Task DeleteImageAsync(int imageId)
        {
            var datasetImage = new DatasetImage { Id = imageId };
            _datasetDbContext.DatasetImages.Attach(datasetImage);
            _datasetDbContext.DatasetImages.Remove(datasetImage);
            await _datasetDbContext.SaveChangesAsync();
        }
    }
}