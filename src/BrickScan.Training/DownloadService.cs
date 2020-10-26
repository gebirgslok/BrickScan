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
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BrickScan.Library.Core;
using BrickScan.Library.Core.Dto;
using BrickScan.Training.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrickScan.Training
{
    public class DownloadService : IDownloadService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DownloadService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IConsoleWriter _writer;

        public DownloadService(IHttpClientFactory httpClientFactory,
            ILogger<DownloadService> logger,
            IConfiguration configuration, 
            IConsoleWriter writer)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
            _writer = writer;
        }

        private async Task<ClassesTrainImagesListResult> GetClassesTrainImagesListAsync()
        {
            var list = new List<DatasetClassTrainImagesDto>();

            using (var httpClient = _httpClientFactory.CreateClient())
            {
                var baseAddress = _configuration.GetValue<string>("BrickScanApiBaseUrl");
                httpClient.BaseAddress = new Uri(baseAddress);

                var hasNextPage = true;
                var page = 1;

                while (hasNextPage)
                {
                    var response = await httpClient.GetAsync($"dataset/classes/training-images?page={page}&pageSize=200");
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);

                    if (document.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        var pagedResult = dataElement.ToObject<PagedResult<DatasetClassTrainImagesDto>>();
                        hasNextPage = pagedResult.HasNextPage;
                        list.AddRange(pagedResult.Results);
                        page += 1;
                    }
                    else
                    {
                        //TODO: log; return error message.
                    }
                }
            }

            return new ClassesTrainImagesListResult(true, list);
        }

        private async Task CopyImagesAsync(List<DatasetClassTrainImagesDto> classTrainImagesList, string destinationDirectory)
        {
            var totalNumOfImages = classTrainImagesList
                .SelectMany(x => x.ImageUrls)
                .Count();

            _logger.LogInformation("Copying {TotalNumOfImages} images from {NumberOfClasses} classes to {DestinationDirectory}.", 
                totalNumOfImages,
                classTrainImagesList.Count,
                destinationDirectory);

            _writer.WriteLine($"Copying {totalNumOfImages} images from {classTrainImagesList.Count} classes to {destinationDirectory}.");

            foreach (var dto in classTrainImagesList)
            {
                if (dto.ImageUrls == null)
                {
                    throw new ArgumentNullException(nameof(DatasetClassTrainImagesDto), 
                        $"DTO for {nameof(dto.ClassId)} = {dto.ClassId} does not contain any image URLs.");
                }

                var classId = dto.ClassId;
                var classBasePath = Path.Combine(destinationDirectory, classId.ToString("D6"));
                Directory.CreateDirectory(classBasePath);

                _writer.WriteLine($"Set Directory = {classBasePath} for class ID = {classId}.");
                _logger.LogInformation("Set {Directory} for {ClassId}.", classBasePath, classId);

                await Task.WhenAll(dto.ImageUrls.Select(url => Task.Run(() =>
                {
                    try
                    {
                        var uri = new Uri(url);

                        if (uri.IsFile || uri.IsUnc)
                        {
                            var sourceFilePath = uri.AbsolutePath;
                            var filename = Path.GetFileName(sourceFilePath);
                            var destinationFilePath = Path.Combine(classBasePath, filename);

                            if (!File.Exists(destinationFilePath))
                            {
                                File.Copy(sourceFilePath, destinationFilePath, true);
                                _writer.WriteLineIfVerbose($"Copied image to {destinationFilePath}.");
                                _logger.LogTrace("Set {Directory} for {ClassId}.", classBasePath, classId);
                            }
                            else
                            {
                                _writer.WriteLineIfVerbose($"Image {filename} already exists, nothing copied.");
                            }
                        }
                        else
                        {
                            //Download image.

                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogCritical(e, $"Failed to copy images. Exception message: {e.Message}");
                    }
                })));
            }
        }

        public async Task DownloadAsync(string destinationDirectory)
        {
            var result = await GetClassesTrainImagesListAsync();

            if (result.Success == false)
            {
                _writer.WriteLine("Failed to retrieve 'ClassesTrainImagesList'.");
                _logger.LogError("");
            }

            await CopyImagesAsync(result.DatasetClassTrainImagesList, destinationDirectory);
        }
    }
}