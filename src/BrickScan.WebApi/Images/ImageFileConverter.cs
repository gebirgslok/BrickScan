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
using BrickScan.Library.Core.Extensions;
using Microsoft.AspNetCore.Http;
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

        public async Task<ImageConversionResult> TryConvertAsync(IFormFile? imageFile)
        {
            if (imageFile == null)
            {
                return ImageConversionResult.ConversionFailedResult("No image specified.");
            }

            if (imageFile.Length == 0)
            {
                return ImageConversionResult.ConversionFailedResult($"Empty image (filename = {imageFile.FileName ?? "n/a"}).");
            }

            if (!GetSupportedImageContentTypes().Contains(imageFile.ContentType))
            {
                var message = $"Image content type '{imageFile.ContentType}' on image (filename) " +
                              $"'{imageFile.FileName ?? "n/a"}' is not supported. " +
                              $"Supported content types: {string.Join(',', GetSupportedImageContentTypes())}.";

                return ImageConversionResult.UnsupportedMediaTypeResult(message);
            }

            await using var imageMemoryStream = new MemoryStream();
            await imageFile!.CopyToAsync(imageMemoryStream);
            var rawBytes = imageMemoryStream.ToArray();
            var imageFormat = ImageHelper.GetImageFormat(rawBytes);

            if (!GetSupportedImageFormats().Contains(imageFormat))
            {
                var message = $"Image format '{imageFormat}' is not supported. Supported image formats: {string.Join(",", GetSupportedImageFormats())}.";
                return ImageConversionResult.UnsupportedMediaTypeResult(message);
            }

            return ImageConversionResult.SuccessfulResult(new ImageData(rawBytes, imageFormat));
        }

        public async Task<ImageConversionResult> TryConvertManyAsync(List<IFormFile> imageFiles)
        {
            var index = 0;

            foreach (var imageFile in imageFiles)
            {
                if (imageFile.Length == 0)
                {
                    return ImageConversionResult.ConversionFailedResult($"Empty image (index = {index}, filename = {imageFile.FileName ?? "n/a"}).");
                }

                if (!GetSupportedImageContentTypes().Contains(imageFile.ContentType))
                {
                    var message = $"Image content type '{imageFile.ContentType}' on image (index = {index}, filename " +
                                  $"'{imageFile.FileName ?? "n/a"})' is not supported. " +
                                  $"Supported content types: {string.Join(',', GetSupportedImageContentTypes())}.";
                    return ImageConversionResult.UnsupportedMediaTypeResult(message);
                }

                index += 1;
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
                    var message =
                        $"Image format '{imageFormat}' is not supported on image (index = {i}, filename = {imageFile.FileName.ToStringOrNullOrEmpty()}. " +
                        $"Supported image formats: {string.Join(",", GetSupportedImageFormats())}.";

                    return ImageConversionResult.UnsupportedMediaTypeResult(message);
                }

                imageDataList[i] = new ImageData(rawBytes, imageFormat);
            }

            return ImageConversionResult.SuccessfulResult(imageDataList);
        }
    }
}