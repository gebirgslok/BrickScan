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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BrickScan.WebApi.Images
{
    //TODO: XML doc
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

        /// <summary>
        /// Deletes the image resource with the posted <paramref name="imageId"/>.
        /// </summary>
        /// <param name="imageId">The ID of the image resource to delete.</param>
        /// <returns>No content (204).</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("{imageId}")]
        public async Task<IActionResult> Delete([FromRoute] int imageId)
        {
            await _datasetService.DeleteImageAsync(imageId);
            return NoContent();
        }

        /// <summary>
        /// Gets and returns the image resource for the requested ID (<paramref name="imageId"/>).
        /// </summary>
        /// <param name="imageId">The ID of the image resource.</param>
        /// <returns>Returns the found <see cref="BrickScan.Library.Dataset.Model.DatasetImage"/>> or
        /// <c>null</c> if no resource for <paramref name="imageId"/> was found.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Route("{imageId}")]
        public async Task<IActionResult> Get([FromRoute] int imageId)
        {
            var datasetImage = await _datasetService.FindImageByIdAsync(imageId);

            if (datasetImage == null)
            {
                return new NotFoundObjectResult(new ApiResponse(404, errors: new[] { $"No image exists with ID = {imageId}." }));
            }

            return new OkObjectResult(new ApiResponse(200, data: datasetImage));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
        public async Task<IActionResult> Post([FromForm] IEnumerable<IFormFile> images)
        {
            var conversionResult = await _imageFileConverter.TryConvertManyAsync(images.ToList());
            var datasetImages = await _datasetService.AddUnclassifiedImagesAsync(conversionResult.ImageDataList.ToList());

            return Ok(new ApiResponse(200, data: datasetImages));
        }
    }
}
