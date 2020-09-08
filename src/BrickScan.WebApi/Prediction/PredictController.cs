﻿#region License
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
    [ApiVersion("1.0")]
    [ApiController]
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
        /// Predicts the posted <paramref name="imageFile"/>.
        /// </summary>
        /// <param name="imageFile">Image to to predict.</param>
        /// <remarks>Supported image file formats: <b>JPEG</b> and <b>PNG</b>.</remarks>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Predict([FromForm] IFormFile imageFile)
        {
            var result = await _imageFileConverter.TryConvertAsync(imageFile);

            if (!result.Success)
            {
                return result.ActionResult!;
            }

            var predictionResult = _imagePredictor.Predict(result.ImageDataList.First().RawBytes);

            var scoreT = _configuration.GetValue<double>("AddImageScoreThreshold");
            var score = 0.4;

            if (score < scoreT)
            {
                var datasetImage = _datasetService.AddUnclassifiedImageAsync(result.ImageDataList.First());
            }

            return new OkObjectResult(new ApiResponse(200, data: predictionResult));
        }
    }
}
