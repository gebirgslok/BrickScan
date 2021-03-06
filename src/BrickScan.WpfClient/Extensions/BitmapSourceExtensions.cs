﻿#region License
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
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrickScan.WpfClient.Extensions
{
    internal static class BitmapSourceExtensions
    {
        public static BitmapSource ClipMaxSize(this BitmapSource bitmapSource, int maxWidth, int maxHeight)
        {
            if (bitmapSource.PixelWidth < maxWidth && bitmapSource.PixelHeight < maxHeight)
            {
                return bitmapSource;
            }

            var ratioW = 1.0 * maxWidth /bitmapSource.PixelWidth;
            var ratioH = 1.0 * maxHeight / bitmapSource.PixelHeight;

            var ratio = Math.Max(ratioW, ratioH);
            return new TransformedBitmap(bitmapSource, new ScaleTransform(ratio, ratio));
        }

        public static byte[] ToByteArray(this BitmapSource bitmapSource)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }
    }
}
