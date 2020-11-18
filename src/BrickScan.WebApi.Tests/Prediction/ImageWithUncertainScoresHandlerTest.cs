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

using System.Collections.Generic;
using System.Threading.Tasks;
using BrickScan.Library.Core;
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Model;
using BrickScan.WebApi.Prediction;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BrickScan.WebApiTests.Prediction
{
    public class ImageWithUncertainScoresHandlerTest
    {
        private static ImageWithUncertainScoresHandler Create(ILogger<ImageWithUncertainScoresHandler>? logger = null,
            IConfiguration? configuration = null,
            IDatasetService? datasetService = null)
        {
            return new ImageWithUncertainScoresHandler(logger ?? A.Dummy<ILogger<ImageWithUncertainScoresHandler>>(),
                configuration ?? A.Dummy<IConfiguration>(),
                datasetService ?? A.Dummy<IDatasetService>());
        }

        [Fact]
        public async Task HandleAsync_AddsUnclassifiedImage()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                {"Prediction:KeepImagesWithLowScores",  "true"},
                {"Prediction:AddImageScoreThreshold",  "70.0"},
                {"Prediction:AddImageScoreDiffThreshold",  "60.0"},
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var datasetService = A.Fake<IDatasetService>();
            var imageData = new ImageData(new byte[10], ImageFormat.Png);
            A.CallTo(() => datasetService.AddUnclassifiedImageAsync(imageData))
                .Returns(Task.FromResult(new DatasetImage { Id = 42 }));

            var handler = Create(configuration: configuration, datasetService: datasetService);

            var predictedClasses = new List<PredictedDatasetClassDto>
            {
                new PredictedDatasetClassDto
                {
                    Id = 121,
                    Score = 72.1f,
                    Items = new List<PredictedDatasetItemDto>()
                },
                new PredictedDatasetClassDto
                {
                    Id = 123,
                    Score = 31.6f,
                    Items = new List<PredictedDatasetItemDto>()
                }

            };

            await handler.HandleAsync(predictedClasses, imageData);

            foreach (var predictedDatasetClassDto in predictedClasses)
            {
                Assert.Equal(42, predictedDatasetClassDto.UnconfirmedImageId);
            }
        }
    }
}
