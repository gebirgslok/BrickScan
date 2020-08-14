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

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BrickScan.WebApi.Prediction
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PredictController : ControllerBase
    {
        private readonly IImageFileConverter _imageFileConverter;
        private readonly IImagePredictor _imagePredictor;

        public PredictController(IImageFileConverter imageFileConverter, 
            IImagePredictor imagePredictor)
        {
            _imageFileConverter = imageFileConverter;
            _imagePredictor = imagePredictor;
        }

        [HttpPost]
        public async Task<IActionResult> Predict(IFormFile imageFile)
        {
            var result = await _imageFileConverter.TryConvertAsync(imageFile);

            if (!result.Success)
            {
                return result.ActionResult!;
            }

            var predictionResult = _imagePredictor.Predict(result.ImageBytes!);
            return new OkObjectResult(new ApiResponse(200, data: predictionResult));
        }
    }
}
