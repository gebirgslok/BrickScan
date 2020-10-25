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
using BrickScan.Library.Dataset;
using BrickScan.WebApi.Images;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrickScan.WebApi.Prediction
{
    //TODO: XML DOC
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PredictionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IImageFileConverter _imageFileConverter;
        private readonly IImagePredictor _imagePredictor;
        private readonly IDatasetService _datasetService;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(IImageFileConverter imageFileConverter,
            IImagePredictor imagePredictor,
            IConfiguration configuration,
            IDatasetService datasetService,
            ILogger<PredictionController> logger)
        {
            _imageFileConverter = imageFileConverter;
            _imagePredictor = imagePredictor;
            _configuration = configuration;
            _datasetService = datasetService;
            _logger = logger;
        }

        /// <summary>
        /// Predicts the posted <paramref name="image"/>.
        /// </summary>
        /// <param name="image">Image to to predict.</param>
        /// <remarks>Supported image file formats: <b>JPEG</b> and <b>PNG</b>.</remarks>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Predict([FromForm] IFormFile image)
        {
            _logger.LogInformation("Prediction requested, trying to convert image ({Filename}, {Bytes}, {ContentType})...",
                image.FileName, image.Length, image.ContentType);

            var result = await _imageFileConverter.TryConvertAsync(image);

            _logger.LogInformation("Conversion result = {@ConversionResult} after processing.", result);

            if (!result.Success)
            {
                return result.ActionResult!;
            }

            _logger.LogDebug("Calling Image Predictor...");

            var scoredLabels = _imagePredictor.Predict(result.ImageDataList.First().RawBytes);

            _logger.LogInformation("Predicted {NScoredLabels} scored labels: {ScoredLabels}.",
                scoredLabels.Count, string.Join(",", scoredLabels.Select(x => x.ToString())));

            var indexes = scoredLabels
                .Select(x => int.Parse(x.Label))
                .ToList();

            _logger.LogDebug("Retrieving class data from database...");

            var predictedClasses = await _datasetService.GetPredictedDatasetClassesAsync(indexes);

            var predictedClassesWithLabels = predictedClasses
                .ToDictionary(x => x.Id.ToString(), x => x);

            foreach (var scoredLabel in scoredLabels)
            {
                predictedClassesWithLabels[scoredLabel.Label].Score = scoredLabel.Score;
            }

            predictedClasses.Sort(new PredictedDatasetClassDtoByScoreComparer());

            _logger.LogInformation("Retrieved the following classes: {@PredictedClasses}.", predictedClasses);

            var addImageActive = _configuration.GetValue<bool>("Prediction:AddImageActive");

            if (addImageActive)
            {
                var scoreT = _configuration.GetValue<double>("Prediction:AddImageScoreThreshold");
                var diffT = _configuration.GetValue<double>("Prediction:AddImageScoreDiffThreshold");
                var maxScore = predictedClasses[0].Score;
                var maxScore2 = predictedClasses.Count > 1 ? predictedClasses[1].Score : 0.0f;
                var diff = maxScore - maxScore2;

                _logger.LogInformation("Adding image is active. Checking whether to add image with the following parameters:" + 
                                       Environment.NewLine +
                                       "score Threshold = {ScoreT}, diff threshold = {DiffT}, " +
                                       "max score = {MaxScore}, max score (2nd) = {MaxScore2}," +
                                       "diff = {Diff}.", scoreT, diffT, maxScore, maxScore2, diff);

                if (maxScore < scoreT || diff < diffT)
                {
                    _logger.LogInformation("Adding posted image as 'Unclassified'.");

                    var datasetImage = await _datasetService.AddUnclassifiedImageAsync(result.ImageDataList.First());

                    foreach (var predictedClass in predictedClasses)
                    {
                        predictedClass.UnconfirmedImageId = datasetImage.Id;
                    }
                }
            }

            return new OkObjectResult(new ApiResponse(200, data: predictedClasses));
        }
    }
}
