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
using System.Drawing;
using BrickScan.WpfClient.Extensions;
using OpenCvSharp;

namespace BrickScan.WpfClient.Model
{
    internal class RoiDetector : IRoiDetector
    {
        private const int MAX_MERGE_SPACING = 20;

        private static readonly Dictionary<int, (double, double)> _sensitivityParams = new Dictionary<int, (double, double)>
        {
            {0, (10.0, 20.0)},
            {1, (15.0, 30.0)},
            {2, (20.0, 40.0)},
            {3, (25.0, 50.0)},
            {4, (30.0, 60.0)},
            {5, (35.0, 70.0)},
            {6, (40.0, 80.0)},
            {7, (45.0, 90.0)},
            {8, (50.0, 100.0)},
            {9, (55.0, 110.0)}
        };

        private readonly IUserConfiguration _userConfiguration;

        public RoiDetector(IUserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;
        }

        public Rectangle Detect(Mat? image)
        {
            if (image == null)
            {
                return Rectangle.Empty;
            }

            using var grey = image.CvtColor(ColorConversionCodes.BGR2GRAY);
            using var blurred = grey.Blur(new OpenCvSharp.Size(5, 5));
            var (t1, t2) = _sensitivityParams[_userConfiguration.SelectedSensitivityLevel];
            using var edge = blurred.Canny(t1, t2);

            using var morph = edge.MorphologyEx(MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(5, 5)),
                iterations: 2, borderType: BorderTypes.Isolated);

            var ccs = morph.ConnectedComponentsEx();

            //ccs.Blobs[0] is the image.
            if (ccs.Blobs.Count <= 1)
            {
                return Rectangle.Empty;
            }

            var largestBlob = ccs.GetLargestBlob();

            if (largestBlob.Width < 30 || largestBlob.Height < 30)
            {
                return Rectangle.Empty;
            }

            var r = largestBlob.Rect;

            for (var i = 1; i < ccs.Blobs.Count; i++)
            {
                var blob = ccs.Blobs[i];

                var (sx, sy) = r.GetSpacings(blob.Rect);

                if (sx < MAX_MERGE_SPACING && sy < MAX_MERGE_SPACING)
                {
                    r = r.Union(blob.Rect);
                }
            }

            var rectangle = r.ToRectangle().MakeSquare();
            rectangle.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            return rectangle;
        }
    }
}