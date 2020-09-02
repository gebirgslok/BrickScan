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
        private static string[]? _supportedImageContentTypes;
        private static ImageFormat[]? _supportedImageFormats;
        private readonly IConfiguration _configuration;

        public ImageFileConverter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string[] GetSupportedImageContentTypes()
        {
            _supportedImageContentTypes ??= _configuration
                .GetSection("SupportedImageContentTypes")
                .Get<string[]>();

            return _supportedImageContentTypes;
        }

        private ImageFormat[] GetSupportedImageFormats()
        {
            _supportedImageFormats ??= _configuration.GetSection("SupportedImageFormats")
                .Get<string[]>()
                .Select(Enum.Parse<ImageFormat>)
                .ToArray();

            return _supportedImageFormats;
        }

        private ImageValidationResult ValidateImageFile(IFormFile? imageFile)
        {
            if (imageFile == null)
            {
                return new ImageValidationResult(false, 400, "No image specified.");
            }

            if (imageFile.Length == 0)
            {
                return new ImageValidationResult(false, 400, $"Empty image (filename = {imageFile.FileName ?? "n/a"}).");
            }

            if (!GetSupportedImageContentTypes().Contains(imageFile.ContentType))
            {
                var message = $"Image content type '{imageFile.ContentType}' on image (filename) " +
                              $"'{imageFile.FileName ?? "n/a"}' is not supported. " +
                              $"Supported content types: {string.Join(',', GetSupportedImageContentTypes())}.";

                return new ImageValidationResult(false, 415, message);
            }

            return new ImageValidationResult(true, 200, string.Empty);
        }

        private ImageValidationResult ValidateImageFiles(IEnumerable<IFormFile> imageFiles)
        {
            var index = 0;

            foreach (var imageFile in imageFiles)
            {
                if (imageFile.Length == 0)
                {
                    return new ImageValidationResult(false, 400, $"Empty image (index = {index}, filename = {imageFile.FileName ?? "n/a"}).");
                }

                if (!GetSupportedImageContentTypes().Contains(imageFile.ContentType))
                {
                    var message = $"Image content type '{imageFile.ContentType}' on image (index = {index}, filename " +
                                  $"'{imageFile.FileName ?? "n/a"})' is not supported. " +
                                  $"Supported content types: {string.Join(',', GetSupportedImageContentTypes())}.";

                    return new ImageValidationResult(false, 415, message);
                }

                index += 1;
            }

            return new ImageValidationResult(true, 200, string.Empty);
        }

        public async Task<ImageConversionResult> TryConvertAsync(IFormFile? imageFile)
        {
            var validationResult =ValidateImageFile(imageFile);

            if (!validationResult.Success)
            {
                return new ImageConversionResult(false, (ImageData?)null, new BadRequestObjectResult(new ApiResponse(validationResult.StatusCode,
                    errors: validationResult.Errors)));
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

                return new ImageConversionResult(false, (ImageData?)null, new ObjectResult(new ApiResponse(415, errors: errors))
                {
                    StatusCode = 415
                });
            }

            return new ImageConversionResult(true, new ImageData(rawBytes, imageFormat), null);
        }

        public async Task<ImageConversionResult> TryConvertManyAsync(List<IFormFile> imageFiles)
        {
            var validationResult = ValidateImageFiles(imageFiles);

            if (!validationResult.Success)
            {
                return new ImageConversionResult(false, (ImageData?)null, new BadRequestObjectResult(new ApiResponse(validationResult.StatusCode,
                    errors: validationResult.Errors)));
            }

            var imageDataList = new ImageData[imageFiles.Count];

            for (var i = 0; i < imageFiles.Count; i++)
            {
                var imageFile = imageFiles[i];
                await using var imageMemoryStream = new MemoryStream();
                await imageFile.CopyToAsync(imageMemoryStream);
                var rawBytes = imageMemoryStream.ToArray();
                var imageFormat = ImageHelper.GetImageFormat(rawBytes);

                if (!GetSupportedImageFormats().Contains(imageFormat))
                {
                    var errors = new List<string>
                    {
                        $"Image format '{imageFormat}' is not supported on image (index = {i}, filename = {imageFile.FileName ?? "null"}. " +
                        $"Supported image formats: {string.Join(",", GetSupportedImageFormats())}."
                    };

                    return new ImageConversionResult(false, (ImageData?)null, new ObjectResult(new ApiResponse(415, errors: errors))
                    {
                        StatusCode = 415
                    });
                }

                imageDataList[i] = new ImageData(rawBytes, imageFormat);
            }

            return new ImageConversionResult(true, imageDataList, null);
        }
    }
}