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
using BrickScan.Library.Dataset;
using BrickScan.Library.Dataset.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BrickScan.WebApi.Images
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageFileConverter _imageFileConverter;
        private readonly IDatasetService _datasetService;

        public ImagesController(IImageFileConverter imageFileConverter,
            IDatasetService datasetService)
        {
            _imageFileConverter = imageFileConverter;
            _datasetService = datasetService;
        }

        private ProblemDetails ImageNotFoundProblemDetails(int imageId) => new ProblemDetails
        {
            Detail = $"Image for ID = {imageId} not found.",
            Instance = HttpContext.Request.Path,
            Status = 404,
            Title = "Image not found",
            Type = "https://httpstatuses.com/404",
            Extensions = { { "traceId", HttpContext.TraceIdentifier } }
        };

        /// <summary>
        /// Deletes the image resource with the posted <paramref name="imageId"/>.
        /// </summary>
        /// <param name="imageId">The ID of the image resource to delete.</param>
        /// <returns>No content (204) if successful or problem details.</returns>
        /// <response code="204">Image deleted successfully.</response>
        /// <response code="404">No image found for the posted image ID..</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpDelete("{imageId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        [Authorize(Policy = Policies.RequiresTrustedUser)]
        public async Task<IActionResult> DeleteImageAsync([FromRoute] int imageId)
        {
            var success = await _datasetService.DeleteImageAsync(imageId);

            if (!success)
            {
                return new NotFoundObjectResult(ImageNotFoundProblemDetails(imageId));
            }

            return NoContent();
        }

        /// <summary>
        /// Gets and returns the image resource for the requested ID (<paramref name="imageId"/>).
        /// </summary>
        /// <param name="imageId">The ID of the image resource.</param>
        /// <returns>Returns the found <see cref="BrickScan.Library.Dataset.Model.DatasetImage"/>> or
        /// problem details else.</returns>
        /// <response code="200">Image was returned successfully.</response>
        /// <response code="404">No image found for the posted image ID..</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatasetImage))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        [Route("{imageId}")]
        public async Task<IActionResult> GetImageByIdAsync([FromRoute] int imageId)
        {
            var datasetImage = await _datasetService.FindImageByIdAsync(imageId);

            if (datasetImage == null)
            {
                return new NotFoundObjectResult(ImageNotFoundProblemDetails(imageId));
            }

            return new OkObjectResult(datasetImage);
        }

        /// <summary>
        /// Uploads one or many image files.
        /// </summary>
        /// <param name="images">A list of image files to upload.</param>
        /// <returns></returns>
        /// <response code="200">Image(s) uploaded successfully.</response>
        /// <response code="400">Failed to convert (at least one) image(s).</response>
        /// <response code="415">One or many images have an unsupported media type. Supported formats: <b>JPEG</b> and <b>PNG</b>.</response>
        /// <response code="500">Internal server error occurred.</response>
        [Authorize]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DatasetImage[]))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> UploadImagesAsync(IEnumerable<IFormFile> images)
        {
            var conversionResult = await _imageFileConverter.TryConvertManyAsync(images.ToList());

            if (!conversionResult.Success)
            {
                return conversionResult.GetActionResult(HttpContext.Request.Path, HttpContext.TraceIdentifier);
            }

            var imageDataList = conversionResult.ImageDataList!.ToArray();

            if (imageDataList.Length == 1)
            {
                var datasetImage = await _datasetService.AddUnclassifiedImageAsync(imageDataList.First());
                return Ok(new[] { datasetImage });
            }

            var datasetImages = await _datasetService.AddUnclassifiedImagesAsync(conversionResult.ImageDataList.ToList());
            return Ok(datasetImages);
        }
    }
}
