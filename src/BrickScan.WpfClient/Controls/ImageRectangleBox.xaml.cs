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

using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BrickScan.WpfClient.Controls
{
    internal partial class ImageRectangleBox
    {
        private static readonly DependencyProperty _imageSourceProperty = DependencyProperty.Register(
            nameof(ImageSource),
            typeof(ImageSource),
            typeof(ImageRectangleBox),
            new PropertyMetadata(OnImageSourceChanged));

        private static readonly DependencyProperty _rectangleProperty = DependencyProperty.Register(nameof(Rectangle),
            typeof(System.Drawing.Rectangle),
            typeof(ImageRectangleBox),
            new PropertyMetadata(OnRectangleChanged));

        private static readonly DependencyProperty _strokeProperty = DependencyProperty.Register(nameof(Stroke),
            typeof(Brush),
            typeof(ImageRectangleBox),
            null);

        private (double x, double y, double sx, double sy) _transform;

        public ImageSource? ImageSource
        {
            get => (ImageSource)GetValue(_imageSourceProperty);
            set => SetValue(_imageSourceProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(_strokeProperty);
            set => SetValue(_strokeProperty, value);
        }

        public System.Drawing.Rectangle Rectangle
        {
            get => (System.Drawing.Rectangle)GetValue(_rectangleProperty);
            set => SetValue(_rectangleProperty, value);
        }

        public double StrokeThickness { get; set; }

        public ImageRectangleBox()
        {
            InitializeComponent();
            LayoutRoot.DataContext = this;
            _transform = GetTransform();
        }

        private static bool IsTransformEmpty((double x, double y, double sx, double sy) transform)
        {
            return !(transform.x > 0.0 || transform.y > 0.0 || transform.sx > 0.0 || transform.sy > 0.0);
        }

        private static (double x, double y, double sx, double sy) CalculateSrcDispTransform(double ws, double hs, double wd, double hd)
        {
            var r1 = wd / hd;
            var r2 = ws / hs;
            var scale = r1 < r2 ? wd / ws : hd / hs;
            var wa = scale * ws;
            var ha = scale * hs;
            var x = (wd - wa) / 2;
            var y = (hd - ha) / 2;
            var sx = wa / ws;
            var sy = ha / hs;
            return (x, y, sx, sy);
        }

        private (double x, double y, double sx, double sy) GetTransform()
        {
            if (DisplayImage?.Source != null)
            {
                var transform = CalculateSrcDispTransform(DisplayImage.Source.Width,
                    DisplayImage.Source.Height,
                    DisplayImage.Width,
                    DisplayImage.Height);

                return transform;
            }

            return (0.0, 0.0, 0.0, 0.0);
        }

        private void DrawRectangle()
        {
            if (Rectangle.IsEmpty || IsTransformEmpty(_transform))
            {
                DisplayRectangle.Visibility = Visibility.Hidden;
                return;
            }

            var x = _transform.x + _transform.sx * Rectangle.X;
            var y = _transform.y + _transform.sy * Rectangle.Y;
            var w = _transform.sx * Rectangle.Width;
            var h = _transform.sy * Rectangle.Height;

            RectangleOffset.X = x;
            RectangleOffset.Y = y;
            DisplayRectangle.Width = w;
            DisplayRectangle.Height = h;
            DisplayRectangle.Visibility = Visibility.Visible;
        }

        private void CalculateTransformAndRedrawRectangle()
        {
            _transform = GetTransform();
            DrawRectangle();
        }

        private static void OnRectangleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is ImageRectangleBox control))
            {
                return;
            }

            control.DrawRectangle();
        }

        private static void OnImageSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is ImageRectangleBox control))
            {
                return;
            }

            control.SetCurrentValue(_imageSourceProperty, e.NewValue as ImageSource);
        }

        private void DisplayImage_OnTargetUpdated(object sender, DataTransferEventArgs e)
        {
            CalculateTransformAndRedrawRectangle();
        }

        private void DisplayImage_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateTransformAndRedrawRectangle();
        }
    }
}
