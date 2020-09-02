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
using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Core;
using BrickScan.Library.Dataset.Model;
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
            var filenameWithoutExt = $"img-{DateTime.UtcNow:yyyyMMdd_HHmmssfff}-{Guid.NewGuid().ToString().Take(4)}";

            _logger.LogDebug("Adding unclassified image data ({ByteCount} bytes, format: {ImageFormat}, created filename: {Filename}).", 
                imageData.RawBytes.Length, imageData.Format, filenameWithoutExt);

            var uri = await _storageService.StoreImageAsync(imageData, filenameWithoutExt);
            var datasetImage = new DatasetImage
            {
                CreatedOn = DateTime.UtcNow,
                Status = EntityStatus.Unclassified,
                Url = uri.AbsolutePath,
            };

            _logger.LogDebug("Adding unclassified datasetImage {@DatasetImage} into database.", datasetImage);

            var entry = await _datasetDbContext.DatasetImages.AddAsync(datasetImage);
            await _datasetDbContext.SaveChangesAsync();
            return entry.Entity;
        }
    }
}