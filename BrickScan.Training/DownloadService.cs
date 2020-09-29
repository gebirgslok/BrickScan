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
        class ClassesTrainImagesListResult
        {
            public bool Success { get; }

            public List<DatasetClassTrainImagesDto> DatasetClassTrainImagesList { get; }

            public ClassesTrainImagesListResult(bool success, List<DatasetClassTrainImagesDto> datasetClassTrainImagesList)
            {
                Success = success;
                DatasetClassTrainImagesList = datasetClassTrainImagesList;
            }
        } 

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DownloadService> _logger;
        private readonly IConfiguration _configuration;

        public DownloadService(IHttpClientFactory httpClientFactory,
            ILogger<DownloadService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        private async Task<ClassesTrainImagesListResult> GetClassesTrainImagesListAsync()
        {
            var list = new List<DatasetClassTrainImagesDto>();
            using (var httpClient = _httpClientFactory.CreateClient())
            {
                var baseAddress = _configuration.GetValue<string>("BrickScanApiBaseUrl");
                httpClient.BaseAddress = new Uri(baseAddress);

                bool hasNextPage = true;

                while (hasNextPage)
                {
                    var response = await httpClient.GetAsync("dataset/classes/training-images");
                    var json = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(json);

                    if (document.RootElement.TryGetProperty("data", out var dataElement))
                    {
                        var pagedResult = dataElement.ToObject<PagedResult<DatasetClassTrainImagesDto>>();
                        hasNextPage = pagedResult.HasNextPage;
                        list.AddRange(pagedResult.Results);
                    }
                }
            }

            return new ClassesTrainImagesListResult(true, list);
        }

        public async Task DownloadAsync(string destinationDirectory)
        {
            //TODO: Query class training images
            var result = await GetClassesTrainImagesListAsync();

            if (result.Success == false)
            {
                //TODO: write console message
                //TODO: log
            }



            //TODO: Download images to local drive
            await Task.Delay(1000);
        }
    }
}