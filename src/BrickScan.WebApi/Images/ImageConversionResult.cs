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
using System.Net;
using BrickScan.Library.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BrickScan.WebApi.Images
{
    public class ImageConversionResult
    {
        public string Message { get; set; } = null!;

        public bool IsUnsupportedMediaType { get; set; }

        public bool Success { get; private set; }

        public IEnumerable<ImageData> ImageDataList { get; private set; } = null!;

        private ImageConversionResult()
        {
        }

        internal IActionResult GetActionResult(string instance, string trace)
        {
            if (Success)
            {
                return new OkResult();
            }

            var statusCode = (int)(IsUnsupportedMediaType ? HttpStatusCode.UnsupportedMediaType : HttpStatusCode.BadRequest);
            var problemDetails = new ProblemDetails
            {
                Detail = Message,
                Instance = instance,
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode),
                Type = $"https://httpstatuses.com/{statusCode}",
            };

            problemDetails.Extensions.Add("traceId", trace);
            

            if (IsUnsupportedMediaType)
            {
                return new ObjectResult(problemDetails)
                {
                    StatusCode = statusCode
                };
            }

            return new BadRequestObjectResult(problemDetails);
        }

        public static ImageConversionResult UnsupportedMediaTypeResult(string? message) => new ImageConversionResult
        {
            Success = false,
            IsUnsupportedMediaType = true,
            ImageDataList = new ImageData[0],
            Message = message ?? "Unsupported image type. Supported formats: JPEG and PNG."
        };

        public static ImageConversionResult SuccessfulResult(ImageData imageData) => new ImageConversionResult
        {
            Success = true,
            IsUnsupportedMediaType = false,
            ImageDataList = new[] { imageData },
            Message = "Image successfully converted."
        };

        public static ImageConversionResult SuccessfulResult(IEnumerable<ImageData> imageDataList) => new ImageConversionResult
        {
            Success = true,
            IsUnsupportedMediaType = false,
            ImageDataList = imageDataList,
            Message = "Images successfully converted."
        };

        public static ImageConversionResult ConversionFailedResult(string message) => new ImageConversionResult
        {
            Success = false,
            IsUnsupportedMediaType = false,
            ImageDataList = new ImageData[0],
            Message = message
        };
    }
}