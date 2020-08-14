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
using System.Text;

namespace BrickScan.WebApi.Extensions
{
    internal static class ImageValidationExtensions
    {
        private enum ImageFormat
        {
            Bmp,
            Jpeg,
            Gif,
            Tiff,
            Png,
            Unknown
        }

        private static ImageFormat GetImageFormat(byte[] bytes)
        {
            var bmp = Encoding.ASCII.GetBytes("BM");
            var gif = Encoding.ASCII.GetBytes("GIF");
            var png = new byte[] { 137, 80, 78, 71 };
            var tiff = new byte[] { 73, 73, 42 };
            var tiff2 = new byte[] { 77, 77, 42 };
            var jpeg = new byte[] { 255, 216, 255, 224 };
            var jpeg2 = new byte[] { 255, 216, 255, 225 };

            if (bmp.SequenceEqual(bytes.Take(bmp.Length)))
            {
                return ImageFormat.Bmp;
            }

            if (gif.SequenceEqual(bytes.Take(gif.Length)))
            {
                return ImageFormat.Gif;
            }

            if (png.SequenceEqual(bytes.Take(png.Length)))
            {
                return ImageFormat.Png;
            }

            if (tiff.SequenceEqual(bytes.Take(tiff.Length)))
            {
                return ImageFormat.Tiff;
            }

            if (tiff2.SequenceEqual(bytes.Take(tiff2.Length)))
            {
                return ImageFormat.Tiff;
            }

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
            {
                return ImageFormat.Jpeg;
            }

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
            {
                return ImageFormat.Jpeg;
            }

            return ImageFormat.Unknown;
        }

        public static bool IsValidImage(this byte[] image, out List<string> errors)
        {
            errors = new List<string>();
            var imageFormat = GetImageFormat(image);
            var isJpegOrPng = imageFormat == ImageFormat.Jpeg || imageFormat == ImageFormat.Png;
            
            if (isJpegOrPng)
            {
                return true;
            }

            errors.Add($"Image format '{imageFormat}' is not supported. " +
                       "Supported image formats: " +
                       $"{ImageFormat.Jpeg}, {ImageFormat.Png}.");
            return false;
        }
    }
}
