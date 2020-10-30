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
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Core.Extensions;
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

        private async Task<Dictionary<int, DatasetImage>> FetchDatasetImages(DatasetClassDto datasetClassDto)
        {
            var distinctImageIds = datasetClassDto.GetAllDistinctImageIds();

            _logger.LogInformation($"{nameof(DatasetClassDto)} contains these distinct IDs: {{@distinctImageIds}}.",
                string.Join(",", distinctImageIds));

            var datasetImages = await _datasetDbContext
                .DatasetImages
                .Where(img => distinctImageIds.Contains(img.Id))
                .ToListAsync();

            var map = datasetImages
                .ToDictionary(img => img.Id, img => img);

            return map;
        }

        private async Task<DatasetClass> AddClassAsync(DatasetClassDto datasetClassDto, string createdBy, EntityStatus status)
        {
            _logger.LogInformation($"Adding {nameof(DatasetClass)} (status = {{Status}}) " +
                                   "with {NumOfTrainImages} train images from user = {CreatedBy}.",
                status,
                datasetClassDto.TrainingImageIds.Count,
                createdBy);

            var map = await FetchDatasetImages(datasetClassDto);

            var datasetClass = new DatasetClass
            {
                CreatedOn = DateTime.Now,
                Status = status,
                CreatedBy = createdBy
            };

            foreach (var trainingImageId in datasetClassDto.TrainingImageIds)
            {
                datasetClass.TrainingImages.Add(map[trainingImageId]);
            }

            foreach (var displayImageId in datasetClassDto.DisplayImageIds)
            {
                datasetClass.DisplayImages.Add(map[displayImageId]);
            }

            foreach (var itemDto in datasetClassDto.Items)
            {
                var datasetItem = new DatasetItem
                {
                    AdditionalIdentifier = itemDto.AdditionalIdentifier,
                    Number = itemDto.Number,
                    DatasetColorId = itemDto.DatasetColorId,
                    DatasetClass = datasetClass
                };

                foreach (var imageId in itemDto.DisplayImageIds)
                {
                    datasetItem.DisplayImages.Add(map[imageId]);
                }

                datasetClass.DatasetItems.Add(datasetItem);
            }

            _logger.LogTrace($"Adding {nameof(DatasetClass)} async...");
            await _datasetDbContext.DatasetClasses.AddAsync(datasetClass);

            foreach (var datasetImage in map.Values)
            {
                _logger.LogDebug($"Setting {nameof(DatasetImage.Status)} of {nameof(DatasetImage)} (ID = {{Id}}) to {{Status}}.",
                    datasetImage.Id,
                    EntityStatus.Inherited);
                datasetImage.Status = EntityStatus.Inherited;
            }

            _logger.LogTrace($"Calling {nameof(DbContext.SaveChangesAsync)} on {nameof(DatasetDbContext)}...");
            var numOfWrittenStateEntries = await _datasetDbContext.SaveChangesAsync();

            _logger.LogInformation($"{nameof(numOfWrittenStateEntries)} = " +
                                   $"{{NumOfWrittenStateEntries}} after calling {nameof(DbContext.SaveChangesAsync)}.",
                numOfWrittenStateEntries);

            return datasetClass;
        }

        public async Task<PagedResult<DatasetClassTrainImagesDto>> GetClassTrainImagesListAsync(int page = 1, int pageSize = 200)
        {
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page), page, $"{nameof(page)} must be >= 1 (received {page}).");
            }

            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, $"{nameof(pageSize)} must be >= 1 (received {pageSize}).");
            }

            _logger.LogInformation("Retrieving ClassTrainImagesList with {@PageOptions}...", new { page, pageSize });

            var result = new PagedResult<DatasetClassTrainImagesDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                RowCount = await _datasetDbContext.DatasetClasses.CountAsync(c =>
                    c.Status == EntityStatus.Classified)
            };

            var pageCount = (double)result.RowCount / pageSize;
            result.PageCount = (int)Math.Ceiling(pageCount);

            var skip = (page - 1) * pageSize;

            _logger.LogInformation("Computed page options: Row count = {RowCount}, page count = {PageCount}, skip = {Skip}.", 
                result.RowCount,
                result.PageCount, skip);

            result.Results = await _datasetDbContext.DatasetClasses
                .Where(c => c.Status == EntityStatus.Classified)
                .Skip(skip)
                .Take(pageSize)
                .Select(c => new DatasetClassTrainImagesDto
                {
                    ClassId = c.Id,
                    ImageUrls = c.TrainingImages.Select(img => img.Url).ToList()
                })
                .ToListAsync();

            _logger.LogInformation("#of received results = {NumOfResults}.", result.Results.Count);

            return result;
        }

        public async Task<DatasetClass?> GetClassByIdAsync(int id)
        {
            _logger.LogDebug($"Trying to find {nameof(DatasetClass)} for {nameof(id)}: {{Id}}.", id);
            var datasetClass = await _datasetDbContext.DatasetClasses.FindAsync(id);

            if (datasetClass == null)
            {
                _logger.LogInformation($"No {nameof(DatasetClass)} found for {nameof(id)}: {{Id}}.", id);
                return null;
            }

            return datasetClass;
        }

        public async Task<DatasetClass?> GetClassByNumberAndColorIdPairsAsync(List<Tuple<string, int>> numberColorIdPairs)
        {
            _logger.LogDebug($"Trying to find {nameof(DatasetClass)} " +
                             $"for {nameof(numberColorIdPairs)}: {{NumberColorIdPairs}}.",
                string.Join(",", numberColorIdPairs.Select(x => $"({x.Item1}:{x.Item2})")));

            var numbers = numberColorIdPairs.Select(x => x.Item1);
            var itemsWithMatchingNumbers = await _datasetDbContext
                .DatasetItems
                .Where(x => numbers.Contains(x.Number))
                .ToListAsync();

            var matches = itemsWithMatchingNumbers.Where(x => numberColorIdPairs
                    .Any(x2 => string.Equals(x2.Item1, x.Number) && x2.Item2 == x.DatasetColorId))
                .ToArray();

            if (!matches.Any())
            {
                return null;
            }

            var classIds = matches.Select(x => x.DatasetClassId);
            return await _datasetDbContext
                .DatasetClasses
                .FirstOrDefaultAsync(x => classIds.Contains(x.Id));
        }

        public async Task<DatasetClass> AddUnclassifiedClassAsync(DatasetClassDto datasetClassDto, string createdBy)
        {
            return await AddClassAsync(datasetClassDto, createdBy, EntityStatus.Unclassified);
        }

        public async Task<DatasetClass> AddClassifiedClassAsync(DatasetClassDto datasetClassDto, string createdBy)
        {
            return await AddClassAsync(datasetClassDto, createdBy, EntityStatus.Classified);
        }

        public async Task<DatasetClass> AddRequiresMergeClassAsync(DatasetClassDto datasetClassDto, string createdBy)
        {
            return await AddClassAsync(datasetClassDto, createdBy, EntityStatus.RequiresMerge);
        }

        public async Task<ConfirmUnclassififiedImageResult> ConfirmUnclassififiedImageAsync(int imageId, int classId)
        {
            var datasetClass = await _datasetDbContext.DatasetClasses
                .FirstOrDefaultAsync(x => x.Id == classId);

            if (datasetClass == null)
            {
                return new ConfirmUnclassififiedImageResult(false, null, new List<string>
                {
                    $"No {nameof(DatasetClass)} found for {nameof(classId)} = {classId}."
                });
            }

            var image = await _datasetDbContext.DatasetImages.FirstOrDefaultAsync(x => x.Id == imageId);

            if (image == null)
            {
                return new ConfirmUnclassififiedImageResult(false, null, new List<string>
                {
                    $"No {nameof(DatasetImage)} found for {nameof(imageId)} = {imageId}."
                });
            }

            image.Status = EntityStatus.Inherited;
            image.TrainDatasetClassId = datasetClass.Id;
            var entry = _datasetDbContext.DatasetImages.Update(image);
            await _datasetDbContext.SaveChangesAsync();
            return new ConfirmUnclassififiedImageResult(true, entry.Entity, null);
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

        public async Task<List<DatasetColor>> AddColorsAsync(List<DatasetColor> colors)
        {
            await _datasetDbContext.DatasetColors.AddRangeAsync(colors);
            await _datasetDbContext.SaveChangesAsync();
            return colors;
        }

        public async Task<List<DatasetColor>> GetColorsAsync()
        {
            //return await Task.FromResult(new List<DatasetColor>
            //{
            //    new DatasetColor
            //    {
            //        BricklinkColorId = 1,
            //        BricklinkColorType = "foo",
            //        BricklinkColorName = "Black",
            //        BricklinkColorHtmlCode = "542323FF"
            //    },            
            //    new DatasetColor
            //    {
            //        BricklinkColorId = 2,
            //        BricklinkColorType = "foo",
            //        BricklinkColorName = "White",
            //        BricklinkColorHtmlCode = "18B343FF"
            //    }
            //});

            return await _datasetDbContext.DatasetColors.ToListAsync();
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
            _logger.LogDebug($"Attempting to delete {nameof(DatasetImage)} with ID = {{Id}}.", imageId);

            var datasetImage = await _datasetDbContext.DatasetImages.FirstOrDefaultAsync(x => x.Id == imageId);

            if (datasetImage == null)
            {
                _logger.LogWarning($"No {nameof(DatasetImage)} found for ID = {{Id}}. No data deleted.", imageId);
                return;
            }

            var uri = new Uri(datasetImage.Url);

            _logger.LogInformation($"Removed {nameof(DatasetImage)} with URL = {{Url}} and ID = {{Id}}.", datasetImage.Url, datasetImage.Id);

            _datasetDbContext.DatasetImages.Remove(datasetImage);
            await _datasetDbContext.SaveChangesAsync();

            _logger.LogDebug("Deleting image from {ImageUri}...", uri);

            await _storageService.DeleteImageAsync(uri);

            _logger.LogDebug("Successfully deleted image from {ImageUri}...", uri);
        }

        public async Task<List<PredictedDatasetClassDto>> GetPredictedDatasetClassesAsync(List<int> datasetClassesIndexes)
        {
            return await _datasetDbContext.DatasetClasses.Where(x => datasetClassesIndexes.Contains(x.Id))
                .Select(x => new PredictedDatasetClassDto
                {
                    DisplayImageUrls = x.DisplayImages.Select(dispImg => dispImg.Url).ToList(),
                    Id = x.Id,
                    Items = x.DatasetItems.Select(item => new PredictedDatasetItemDto
                    {
                        AdditionalIdentifier = item.AdditionalIdentifier,
                        Number = item.Number,
                        DisplayImageUrls = item.DisplayImages.Select(i2 => i2.Url).ToList(),
                        Color = item.DatasetColor != null ? new ColorDto
                        {
                            BricklinkColorId = item.DatasetColor.BricklinkColorId,
                            BricklinkColorName = item.DatasetColor.BricklinkColorName,
                            BricklinkColorHtmlCode = item.DatasetColor.BricklinkColorHtmlCode,
                            BricklinkColorType = item.DatasetColor.BricklinkColorType,
                            Id = item.Id
                        } : null
                    }).ToList()
                })
                .ToListAsync();
        }

        public async Task<DatasetColor> AddColorAsync(ColorDto color)
        {
            _logger.LogInformation("Adding {@Color} to database...", color);

            var doesBricklinkIdAlreadyExist = await _datasetDbContext
                .DatasetColors
                .AnyAsync(x => x.BricklinkColorId == color.BricklinkColorId);

            if (doesBricklinkIdAlreadyExist)
            {
                throw new InvalidOperationException($"An {nameof(DatasetColor)} entry with bricklink Id = {color.BricklinkColorId} already exists. " +
                                                    "Cannot add color.");
            }

            var datasetColor = new DatasetColor
            {
                BricklinkColorId = color.BricklinkColorId,
                BricklinkColorName = color.BricklinkColorName,
                BricklinkColorType = color.BricklinkColorType,
                BricklinkColorHtmlCode = color.BricklinkColorHtmlCode
            };

            await _datasetDbContext.DatasetColors.AddAsync(datasetColor);
            await _datasetDbContext.SaveChangesAsync();

            _logger.LogInformation($"Created new {nameof(DatasetColor)} entry with ID = {{Id}}.", datasetColor.Id);

            return datasetColor;
        }
    }
}