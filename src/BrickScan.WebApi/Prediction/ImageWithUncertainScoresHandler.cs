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
using System.Threading.Tasks;
using BrickScan.Library.Core;
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Dataset;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrickScan.WebApi.Prediction
{
    public class ImageWithUncertainScoresHandler : IPredictedClassesHandler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ImageWithUncertainScoresHandler> _logger;
        private readonly IDatasetService _datasetService;

        public ImageWithUncertainScoresHandler(ILogger<ImageWithUncertainScoresHandler> logger, 
            IConfiguration configuration, 
            IDatasetService datasetService)
        {
            _logger = logger;
            _configuration = configuration;
            _datasetService = datasetService;
        }

        public async Task HandleAsync(List<PredictedDatasetClassDto> predictedClasses, ImageData imageData)
        {
            var keepImagesWithLowScores = _configuration.GetValue<bool>("Prediction:KeepImagesWithLowScores");

            if (keepImagesWithLowScores)
            {
                var scoreT = _configuration.GetValue<double>("Prediction:AddImageScoreThreshold");
                var diffT = _configuration.GetValue<double>("Prediction:AddImageScoreDiffThreshold");
                var maxScore = predictedClasses[0].Score;
                var maxScore2 = predictedClasses.Count > 1 ? predictedClasses[1].Score : 0.0f;
                var diff = maxScore - maxScore2;

                _logger.LogInformation(
                    "KeepImagesWithLowScores is enabled. Checking whether to add image with the following parameters:" +
                    Environment.NewLine +
                    "score threshold = {ScoreT}, diff threshold = {DiffT}, " +
                    "max score = {MaxScore}, max score (2nd) = {MaxScore2}," +
                    "diff = {Diff}.", scoreT, diffT, maxScore, maxScore2, diff);

                if (maxScore < scoreT || diff < diffT)
                {
                    _logger.LogInformation("Adding posted image as 'Unclassified'.");

                    var datasetImage = await _datasetService.AddUnclassifiedImageAsync(imageData);

                    foreach (var predictedClass in predictedClasses)
                    {
                        predictedClass.UnconfirmedImageId = datasetImage.Id;
                    }
                }
            }
        }
    }
}