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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using BricklinkSharp.Client;
using BrickScan.WpfClient.Model;
using Microsoft.Xaml.Behaviors.Media;

namespace BrickScan.WpfClient.Controls
{
    internal partial class BlCatalogItemDisplayBox
    {
        private static readonly DependencyProperty _catalogItemProperty = DependencyProperty.Register(
            nameof(CatalogItem),
            typeof(CatalogItem),
            typeof(BlCatalogItemDisplayBox),
            new PropertyMetadata(OnDatasetItemChanged));

        public CatalogItem? CatalogItem
        {
            get => (CatalogItem)GetValue(_catalogItemProperty);
            set => SetValue(_catalogItemProperty, value);
        }

        public BlCatalogItemDisplayBox()
        {
            InitializeComponent();
        }

        private static string GetDimensions(CatalogItem item)
        {
            if (item.DimX == 0 || item.DimY == 0)
            {
                return "?";
            }

            if (item.DimZ == 0)
            {
                return $"{item.DimX} × {item.DimY}";
            }

            return $"{item.DimX} × {item.DimY} × {item.DimZ}";
        }

        private static void OnDatasetItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is BlCatalogItemDisplayBox control))
            {
                return;
            }

            if (!(e.NewValue is CatalogItem item))
            {
                return;
            }

            control.SetCurrentValue(_catalogItemProperty, item);
            control.Title.Text = $"{item.Number} - {item.Name}";
            control.Dimensions.Text = GetDimensions(item);
            control.Weight.Text = $"{item.Weight:F2} g";
            control.Year.Text = item.YearReleased.ToString();
            var url = item.ImageUrl;
            //control.ThumbnailImage.Source = url == null ? null : new BitmapImage(new Uri($"{item.ImageUrl}"));
            control.ThumbnailImage.Source = url == null ? null : new BitmapImage(new Uri($"https:{item.ImageUrl}"));
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = BricklinkHelper.GeneratePartUrl(CatalogItem!.Number);
                Process.Start(url);
            }
            catch
            {
                //Ignored.
            }
        }
    }
}
