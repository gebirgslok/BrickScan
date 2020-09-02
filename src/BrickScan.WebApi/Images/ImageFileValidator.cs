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
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace BrickScan.WebApi.Images
{
    internal class ImageFileValidator : IImageFileValidator
    {
        private static string[]? _supportedImageContentTypes;
        private readonly IConfiguration _configuration;

        public ImageFileValidator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string[] GetSupportedImageContentTypes()
        {
            _supportedImageContentTypes ??= _configuration.GetValue<string[]>("SupportedImageContentTypes");
            return _supportedImageContentTypes;
        }

        public ImageValidationResult ValidateImageFiles(IEnumerable<IFormFile> imageFile)
        {
            throw new NotImplementedException();
        }

        public ImageValidationResult ValidateImageFile(IFormFile? imageFile)
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
    }
}