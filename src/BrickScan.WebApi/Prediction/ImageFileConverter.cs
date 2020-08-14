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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BrickScan.WebApi.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BrickScan.WebApi.Prediction
{
    internal class ImageFileConverter : IImageFileConverter
    {
        private static readonly string[] _allowedImageContentTypes = { "image/png", "image/jpeg" };

        public async Task<ImageConversionResult> TryConvertAsync(IFormFile imageFile)
        {
            if (imageFile == null)
            {
                return new ImageConversionResult(false, null, new BadRequestObjectResult(new ApiResponse(400, errors: new List<string>
                {
                    "No image specified."

                })));
            }

            if (imageFile.Length == 0)
            {
                return new ImageConversionResult(false, null, new BadRequestObjectResult(new ApiResponse(400, errors: new List<string>
                {
                    "Empty image."

                })));
            }

            if (!_allowedImageContentTypes.Contains(imageFile.ContentType))
            {
                return new ImageConversionResult(false, null, new ObjectResult(new ApiResponse(415, errors: new List<string>
                {
                    $"Image content type '{imageFile.ContentType}' is not supported. Supported content types: {string.Join(',', _allowedImageContentTypes)}"
                }))
                {
                    StatusCode = 415
                });
            }

            await using var imageMemoryStream = new MemoryStream();
            await imageFile.CopyToAsync(imageMemoryStream);
            var imageData = imageMemoryStream.ToArray();

            if (!imageData.IsValidImage(out var errors))
            {
                return new ImageConversionResult(false, null, new ObjectResult(new ApiResponse(415, errors: errors)) { StatusCode = 415 });
            }

            return new ImageConversionResult(true, imageData, null);
        }
    }
}