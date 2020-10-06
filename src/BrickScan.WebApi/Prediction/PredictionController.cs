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

using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Dataset;
using BrickScan.WebApi.Images;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

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

        public PredictionController(IImageFileConverter imageFileConverter,
            IImagePredictor imagePredictor,
            IConfiguration configuration,
            IDatasetService datasetService)
        {
            _imageFileConverter = imageFileConverter;
            _imagePredictor = imagePredictor;
            _configuration = configuration;
            _datasetService = datasetService;
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
            //TODO: Add logging.
            var result = await _imageFileConverter.TryConvertAsync(image);

            if (!result.Success)
            {
                return result.ActionResult!;
            }

            var scoredLabels = _imagePredictor.Predict(result.ImageDataList.First().RawBytes);

            var indexes = scoredLabels
                .Select(x => int.Parse(x.Label))
                .ToList();

            var predictedClasses = await _datasetService.GetPredictedDatasetClassesAsync(indexes);

            var predictedClassesWithLabels = predictedClasses
                .ToDictionary(x => x.Id.ToString(), x => x);

            foreach (var scoredLabel in scoredLabels)
            {
                predictedClassesWithLabels[scoredLabel.Label].Score = scoredLabel.Score;
            }

            predictedClasses.Sort(new PredictedDatasetClassDtoByScoreComparer());

            var scoreT = _configuration.GetValue<double>("Prediction:AddImageScoreThreshold");
            var minScore = predictedClasses.Min(x => x.Score);

            if (minScore < scoreT)
            {
                var datasetImage = await _datasetService.AddUnclassifiedImageAsync(result.ImageDataList.First());
                
                foreach (var predictedClass in predictedClasses)
                {
                    predictedClass.UnconfirmedImageId = datasetImage.Id;
                }
            }

            return new OkObjectResult(new ApiResponse(200, data: predictedClasses));
        }
    }
}
