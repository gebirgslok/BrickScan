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
using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Core.Dto;
using BrickScan.Library.Core.Extensions;
using BrickScan.Library.Dataset;
using BrickScan.WebApi.Images;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BrickScan.WebApi.Prediction
{
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    public class PredictionController : ControllerBase
    {
        private readonly IImageFileConverter _imageFileConverter;
        private readonly IImagePredictor _imagePredictor;
        private readonly IDatasetService _datasetService;
        private readonly ILogger<PredictionController> _logger;
        private readonly IEnumerable<IPredictedClassesHandler> _handlers;

        public PredictionController(IImageFileConverter imageFileConverter,
            IImagePredictor imagePredictor,
            IDatasetService datasetService,
            ILogger<PredictionController> logger, 
            IEnumerable<IPredictedClassesHandler> handlers)
        {
            _imageFileConverter = imageFileConverter;
            _imagePredictor = imagePredictor;
            _datasetService = datasetService;
            _logger = logger;
            _handlers = handlers;
        }

        /// <summary>
        /// Predicts the posted <paramref name="image"/>.
        /// </summary>
        /// <param name="image">Image to to predict.</param>
        /// <remarks>Supported image file formats: <b>JPEG</b> and <b>PNG</b>.</remarks>
        /// <response code="200">Returns a sorted list (descending by score) of matching class candidates.</response>
        /// <response code="400">Invalid image file.</response>
        /// <response code="415">Unsupported media type of the posted image. Supported formats: <b>JPEG</b> and <b>PNG</b>.</response>
        /// <response code="429">Too many requests. Allowed quota: 1 call per every 3 seconds.</response>
        /// <response code="500">Internal server error occurred.</response>
        /// <returns>Returns a sorted list (descending by score) of matching class candidates
        /// if successful or a problem details object else.</returns>
        [HttpPost("predict")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PredictedDatasetClassDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> PredictAsync(IFormFile? image)
        {
            _logger.LogInformation("Prediction requested, trying to convert image ({Filename}, {Bytes}, {ContentType})...",
                image?.FileName.ToStringOrNullOrEmpty(), 
                image?.Length.ToStringOrNullOrEmpty(),
                image?.ContentType.ToStringOrNullOrEmpty());

            var result = await _imageFileConverter.TryConvertAsync(image);

            _logger.LogInformation("Conversion result = {@ConversionResult} after processing.", result);

            if (!result.Success)
            {
                return result.GetActionResult(HttpContext.Request.Path, HttpContext.TraceIdentifier);
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

            foreach (var handler in _handlers)
            {
                await handler.HandleAsync(predictedClasses, result.ImageDataList.First());
            }

            return new OkObjectResult(predictedClasses);
        }
    }
}
