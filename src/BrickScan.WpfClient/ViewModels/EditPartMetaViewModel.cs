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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public class EditPartMetaViewModel : PropertyChangedBase
    {
        public bool UseFirstTrainImageAsDisplayImage { get; set; } = true;

        public BindableCollection<BitmapSource> TrainImages { get; }

        [DependsOn(nameof(UseFirstTrainImageAsDisplayImage), nameof(ExtraDisplayImage))]
        public IEnumerable<BitmapSource> DisplayImages => GetDisplayImages();

        public BitmapSource? ExtraDisplayImage { get; private set; }

        [DependsOn(nameof(ExtraDisplayImage), nameof(UseFirstTrainImageAsDisplayImage))]
        public bool CanRemoveExtraDisplayImage => ExtraDisplayImage != null && UseFirstTrainImageAsDisplayImage;

        [DependsOn(nameof(UseFirstTrainImageAsDisplayImage), nameof(ExtraDisplayImage))]
        public bool CanToggleUseFirstTrainImageAsDisplayImage =>
            !UseFirstTrainImageAsDisplayImage || ExtraDisplayImage != null;

        public BindableCollection<PartConfigViewModel> PartConfigViewModels { get; }

        public EditPartMetaViewModel(IEnumerable<BitmapSource> trainImages)
        {
            TrainImages = new BindableCollection<BitmapSource>(trainImages);
            PartConfigViewModels = new BindableCollection<PartConfigViewModel>();
            PartConfigViewModels.Add(new PartConfigViewModel());
        }

        private IEnumerable<BitmapSource> GetDisplayImages()
        {
            var displayImages = new List<BitmapSource>();

            if (UseFirstTrainImageAsDisplayImage)
            {
                displayImages.Add(TrainImages.First());
            }

            if (ExtraDisplayImage != null)
            {
                displayImages.Add(ExtraDisplayImage);
            }

            return displayImages;
        }

        public void RemoveExtraDisplayImage()
        {
            ExtraDisplayImage = null;
        }

        public void SelectImage()
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Filter = $"{Properties.Resources.ImageFiles} (*.jpg, *.jpeg, *.png) | *.jpg; *.jpeg; *.png"
            };

            var wasSelected = dialog.ShowDialog();

            if (wasSelected.HasValue && wasSelected.Value)
            {
                var imagePath = dialog.FileName;
                ExtraDisplayImage = new BitmapImage(new Uri(imagePath));
            }
        }
    }
}
