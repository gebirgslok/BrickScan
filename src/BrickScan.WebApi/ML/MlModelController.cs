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
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BrickScan.WebApi.ML
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class MlModelController : ControllerBase
    {
        private static readonly string _supportedContentType = "application/zip";
        private readonly IConfiguration _configuration;
        private readonly ILogger<MlModelController> _logger;

        public MlModelController(IConfiguration configuration, ILogger<MlModelController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private async Task CopyAsync(byte[] bytes, string modelFilePath)
        {
            var absoluteFilePath = PathHelpers.GetAbsolutePath(modelFilePath, AppDomain.CurrentDomain.BaseDirectory!);
            var destinationDirectory = Path.GetDirectoryName(absoluteFilePath);

            if (!Directory.Exists(destinationDirectory))
            {
                _logger.LogInformation("Creating {DestinationDirectory} (does not exist yet)...", destinationDirectory);
                Directory.CreateDirectory(destinationDirectory); 
                _logger.LogInformation("Successfully created {DestinationDirectory}.", destinationDirectory);
            }

            _logger.LogInformation("Saving MlModel.zip ({Size} bytes) to {ModelFilePath}...", bytes.Length, absoluteFilePath);
            await System.IO.File.WriteAllBytesAsync(absoluteFilePath, bytes);
            _logger.LogInformation("Successfully saved MlModel.zip.", bytes.Length, absoluteFilePath);
        }

        [HttpPost]
        [RequestSizeLimit(157286400)] //150 MB
        public async Task<IActionResult> Upload(IFormFile mlModel)
        {
            if (mlModel == null)
            {
                return new BadRequestObjectResult(new ApiResponse(400, errors: new List<string>
                {
                    "No MlModel.zip file specified."
                }));
            }

            if (mlModel.Length == 0)
            {
                return new BadRequestObjectResult(new ApiResponse(400, errors: new List<string>
                {
                    "Empty MlModel.zip file."
                }));
            }

            if (mlModel.ContentType != _supportedContentType)
            {
                return new ObjectResult(new ApiResponse(415, errors: new List<string>
                {
                    $"Invalid content type = '{mlModel.ContentType}. Supported content type: {_supportedContentType}."
                }))
                {
                    StatusCode = 415
                };
            }

            await using var imageMemoryStream = new MemoryStream();
            await mlModel.CopyToAsync(imageMemoryStream);
            var bytes = imageMemoryStream.ToArray();

            await CopyAsync(bytes, _configuration.GetValue<string>("ModelFilePath"));
            return new OkResult();
        }
    }
}
