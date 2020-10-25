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
using System.Text.Json.Serialization;
using BrickScan.Library.Core;
using Microsoft.AspNetCore.Mvc;

namespace BrickScan.WebApi.Images
{
    public class ImageConversionResult
    {
        [JsonIgnore]
        public IActionResult? ActionResult { get; }

        public bool Success { get; }

        public IEnumerable<ImageData> ImageDataList { get; }

        public ImageConversionResult(bool success, IEnumerable<ImageData> imageData, IActionResult? actionResult)
        {
            Success = success;
            ImageDataList = imageData;
            ActionResult = actionResult;
        }

        internal ImageConversionResult(bool success, ImageData? imageData, IActionResult? actionResult)
        {
            Success = success;
            ImageDataList = imageData == null ? new ImageData[0] : new[] { imageData };
            ActionResult = actionResult;
        }
    }
}