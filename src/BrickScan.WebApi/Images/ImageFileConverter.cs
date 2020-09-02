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
using System.Linq;
using System.Threading.Tasks;
using BrickScan.Library.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BrickScan.WebApi.Images
{
    internal class ImageFileConverter : IImageFileConverter
    {
        private static ImageFormat[]? _supportedImageFormats;
        private readonly IImageFileValidator _imageFileValidator;
        private readonly IConfiguration _configuration;

        public ImageFileConverter(IConfiguration configuration, IImageFileValidator imageFileValidator)
        {
            _configuration = configuration;
            _imageFileValidator = imageFileValidator;
        }

        private ImageFormat[] GetSupportedImageFormats()
        {
            _supportedImageFormats ??= _configuration.GetValue<string[]>("SupportedImageFormats")
                .Select(Enum.Parse<ImageFormat>).ToArray();
            return _supportedImageFormats;
        }

        public async Task<ImageConversionResult> TryConvertAsync(IFormFile? imageFile)
        {
            var validationResult = _imageFileValidator.ValidateImageFile(imageFile);

            if (!validationResult.Success)
            {
                return new ImageConversionResult(false, null, new BadRequestObjectResult(new ApiResponse(validationResult.StatusCode, errors: new List<string>
                {
                    validationResult.Message
                })));
            }

            await using var imageMemoryStream = new MemoryStream();
            await imageFile!.CopyToAsync(imageMemoryStream);
            var rawBytes = imageMemoryStream.ToArray();
            var imageFormat = ImageHelper.GetImageFormat(rawBytes);

            if (!GetSupportedImageFormats().Contains(imageFormat))
            {
                var errors = new List<string>
                {
                    $"Image format '{imageFormat}' is not supported. Supported image formats: {string.Join(",", GetSupportedImageFormats())}."
                };

                return new ImageConversionResult(false, null, new ObjectResult(new ApiResponse(415, errors: errors))
                {
                    StatusCode = 415
                });
            }

            return new ImageConversionResult(true, new ImageData(rawBytes, imageFormat), null);
        }

        public Task<ImageConversionResult> TryConvertManyAsync(IEnumerable<IFormFile> imageFiles)
        {
            //foreach (var imageFile in imageFiles)
            //{
            //    var validationResult = ValidateImageFile(imageFile);

            //    if (!validationResult.Success)
            //    {
            //        return new ImageConversionResult(false, null, new BadRequestObjectResult(new ApiResponse(validationResult.StatusCode, errors: new List<string>
            //        {
            //            validationResult.Message
            //        })));
            //    }
            //}


            throw new NotImplementedException();
        }
    }
}