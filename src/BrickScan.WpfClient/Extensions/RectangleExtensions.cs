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
using System.Drawing;
using OpenCvSharp;

namespace BrickScan.WpfClient.Extensions
{
    internal static class RectangleExtensions
    {
        private static (int cx, int cy) Center(this Rect r)
        {
            var cx = (r.Right + r.Left) / 2;
            var cy = (r.Bottom + r.Top) / 2;
            return (cx, cy);
        }

        internal static (int sx, int sy) GetSpacings(this Rect r1, Rect r2)
        {
            var (cx1, cy1) = r1.Center();
            var (cx2, cy2) = r2.Center();
            var sx = Math.Abs(cx1 - cx2) - (r1.Width + r2.Width) / 2;
            var sy = Math.Abs(cy1 - cy2) - (r1.Height + r2.Height) / 2;
            return (sx, sy);
        }

        internal static Rectangle ToRectangle(this Rect r)
        {
            return new Rectangle(r.X, r.Y, r.Width, r.Height);
        }

        internal static Rectangle MakeSquare(this Rectangle r)
        {
            if (r.Width > r.Height)
            {
                var diff = r.Width - r.Height;
                var y = r.Y - diff / 2;
                return new Rectangle(r.X, y, r.Width, r.Width);
            }

            if (r.Height > r.Width)
            {
                var diff = r.Height - r.Width;
                var x = r.X - diff / 2;
                return new Rectangle(x, r.Y, r.Height, r.Height);
            }

            return new Rectangle(r.X, r.Y, r.Width, r.Height);
        }
    }
}
