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
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BrickScan.Library.Core.Dto;

namespace BrickScan.WpfClient.Controls
{
    internal partial class DatasetItemDisplayBox
    {
        private static readonly DependencyProperty _datasetItemProperty = DependencyProperty.Register(
            nameof(DatasetItem),
            typeof(PredictedDatasetItemDto),
            typeof(DatasetItemDisplayBox),
            new PropertyMetadata(OnDatasetItemChanged));

        private static readonly DependencyProperty _scoreProperty = DependencyProperty.Register(
            nameof(Score),
            typeof(float?),
            typeof(DatasetItemDisplayBox),
            new PropertyMetadata(OnScoreChanged));

        private int _currentImageIndex;

        private int ImagesCount => DatasetItem?.DisplayImageUrls?.Count ?? 0;

        public PredictedDatasetItemDto? DatasetItem
        {
            get => (PredictedDatasetItemDto)GetValue(_datasetItemProperty);
            set => SetValue(_datasetItemProperty, value);
        }

        private string? GetUrl(int index)
        {
            var urls = DatasetItem?.DisplayImageUrls;

            if (urls != null && urls.Count > index)
            {
                return urls[index];
            }

            return null;
        }

        public float? Score
        {
            get => (float?) GetValue(_scoreProperty);
            set => SetValue(_scoreProperty, value);
        }

        public DatasetItemDisplayBox()
        {
            InitializeComponent();
            UpdateScoreVisibility(Score);
        }

        private void UpdateNextPrevButtonEnabledStates()
        {
            ShowNextButton.IsEnabled = _currentImageIndex < ImagesCount - 1;
            ShowPreviousButton.IsEnabled = _currentImageIndex > 0;
        }

        private void UpdateScoreVisibility(float? score)
        {
            if (score.HasValue)
            {
                ScoreBox.Text = score.Value.ToString("F2", new CultureInfo("en-US"));
                ScoreBox.Visibility = Visibility.Visible;
                ScoreLabel.Visibility = Visibility.Visible;
            }
            else
            {
                ScoreBox.Text = null;
                ScoreBox.Visibility = Visibility.Collapsed;
                ScoreLabel.Visibility = Visibility.Collapsed;
            }
        }

        private void SetSelectedDisplayImage(int index = 0)
        {
            var url = GetUrl(index);
            SelectedDisplayImage.Source = url == null ? null : new BitmapImage(new Uri(url));
            _currentImageIndex = index;
            ImageShowNextPreviousPanel.Visibility = ImagesCount > 1 ? Visibility.Visible : Visibility.Collapsed;
            UpdateNextPrevButtonEnabledStates();
        }

        private static void OnScoreChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is DatasetItemDisplayBox control))
            {
                return;
            }

            control.UpdateScoreVisibility(e.NewValue as float?);
        }

        private static void OnDatasetItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is DatasetItemDisplayBox control))
            {
                return;
            }

            if (!(e.NewValue is PredictedDatasetItemDto item))
            {
                return;
            }

            control.SetCurrentValue(_datasetItemProperty, item);
            control.PartNo.Text = item.Number;
            control.ColorRectangle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(item.Color?.BricklinkColorHtmlCode));
            control.ColorName.Text = item.Color?.BricklinkColorName;
            control.AdditionalIdentifier.Text = item.AdditionalIdentifier;

            var url = item.DisplayImageUrls?.First();
            control.SelectedDisplayImage.Source = url == null ? null : new BitmapImage(new Uri(url));
            control._currentImageIndex = 0;
            var count = item.DisplayImageUrls?.Count ?? 0;
            control.ImageShowNextPreviousPanel.Visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;
            control.UpdateNextPrevButtonEnabledStates();
        }

        private void OnShowNextImageClicked(object sender, RoutedEventArgs e)
        {
            SetSelectedDisplayImage(_currentImageIndex + 1);
        }

        private void OnShowPreviousImageClicked(object sender, RoutedEventArgs e)
        {
            SetSelectedDisplayImage(_currentImageIndex - 1);
        }
    }
}
