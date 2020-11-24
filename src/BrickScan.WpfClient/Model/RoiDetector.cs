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
        class SensitivityParams
        {
            public double T1 { get; }

            public double T2 { get; }

            public int BlurKernelSize { get; }

            public SensitivityParams(double t1, double t2, int blurKernelSize)
            {
                T1 = t1;
                T2 = t2;
                BlurKernelSize = blurKernelSize;
            }
        }

        private const int MAX_MERGE_SPACING = 20;
        private const int DOWN_SAMPLE_WIDTH = 640;
        private const int DOWN_SAMPLE_HEIGHT = 480;

        private static readonly Dictionary<int, SensitivityParams> _sensitivityParams = new Dictionary<int, SensitivityParams>
        {
            {0, new SensitivityParams(40.0, 80.0, 5)},
            {1, new SensitivityParams(38.0, 74.0, 5)},
            {2, new SensitivityParams(36.0, 71.0, 5)},
            {3, new SensitivityParams(34.0, 68.0, 5)},
            {4, new SensitivityParams(33.0, 65.0, 3)},
            {5, new SensitivityParams(31.0, 62.0, 5)},
            {6, new SensitivityParams(30.0, 59.0, 3)},
            {7, new SensitivityParams(28.0, 56.0, 3)},
            {8, new SensitivityParams(27.0, 53.0, 3)},
            {9, new SensitivityParams(25.0, 50.0, 3)},
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

            var sw = 1.0 * image.Width / DOWN_SAMPLE_WIDTH;
            var sh = 1.0 * image.Height / DOWN_SAMPLE_HEIGHT;
            using var downSampled = image.Resize(new OpenCvSharp.Size(DOWN_SAMPLE_WIDTH, DOWN_SAMPLE_HEIGHT));
            using var grey = downSampled.CvtColor(ColorConversionCodes.BGR2GRAY);
            var sensitivityParams = _sensitivityParams[_userConfiguration.SelectedSensitivityLevel];
            using var blurred = grey.Blur(new OpenCvSharp.Size(sensitivityParams.BlurKernelSize, sensitivityParams.BlurKernelSize));
            using var edge = blurred.Canny(sensitivityParams.T1, sensitivityParams.T2);

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

            var rectangle = r.ToRectangle().Scale(sw, sh).MakeSquare();
            rectangle.Intersect(new Rectangle(0, 0, image.Width, image.Height));
            return rectangle;
        }
    }
}