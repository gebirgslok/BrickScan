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
using System.Linq;
using System.Windows.Media.Imaging;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Model;
using Microsoft.Xaml.Behaviors.Core;
using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public class AddPartsViewModel : CameraCaptureBaseViewModel
    {
        private const int MIN_IMAGES_COUNT_TO_PROCEED = 2;
        private const int MAX_IMAGES_COUNT_TO_PROCEED = 20;
        private readonly IEventAggregator _eventAggregator;
        
        static readonly Random _random = new Random();

        public BindableCollection<BitmapSource> Images { get; } = new BindableCollection<BitmapSource>();

        [DependsOn(nameof(Frame), nameof(Rectangle))]
        public bool CanAddImage => true; //Frame != null && !Frame.Empty() && !Rectangle.IsEmpty;

        public bool CanClear => Images.Any();

        public bool CanProceed =>
            Images.Count >= MIN_IMAGES_COUNT_TO_PROCEED && Images.Count <= MAX_IMAGES_COUNT_TO_PROCEED;

        public ActionCommand DeleteImageCommand => new ActionCommand(DeleteImage);

        public ActionCommand MoveTopCommand => new ActionCommand(MoveTop);

        public AddPartsViewModel(IVideoCapture videoCapture, 
            CameraSetupViewModel cameraSetupViewModel, 
            IEventAggregator eventAggregator) : base(videoCapture, cameraSetupViewModel)
        {
            _eventAggregator = eventAggregator;
        }

        public void Clear()
        {
            Images.Clear();
            NotifyOfPropertyChange(nameof(CanClear));
            NotifyOfPropertyChange(nameof(CanProceed));
        }

        public void AddImage()
        {
            var paths = new string[]
            {
                @"C:\Users\eisenbach\Pictures\doge.png",
                @"C:\Users\eisenbach\Pictures\csm_8-sge-borussiaglad118326.03.14aw_220bd1006d.jpg",
                @"C:\Users\eisenbach\Pictures\MX590-P Maske.jpg",
                @"C:\Users\eisenbach\Pictures\label_bsp.png"
            };

            var image = new BitmapImage(new Uri(paths[_random.Next(paths.Length)]));
            Images.Add(image);
            NotifyOfPropertyChange(nameof(CanClear)); 
            NotifyOfPropertyChange(nameof(CanProceed));
        }

        public void DeleteImage(object obj)
        {
            if (obj is BitmapSource image)
            {
                Images.Remove(image);
                NotifyOfPropertyChange(nameof(CanClear)); 
                NotifyOfPropertyChange(nameof(CanProceed));
            }
        }

        public void MoveTop(object obj)
        {
            if (obj is BitmapSource image)
            {
                Images.Move(Images.IndexOf(image), 0);
            }
        }

        public void Proceed()
        {
            _eventAggregator.PublishOnUIThread(new OnEditClassMetaRequested(Images));
        }
    }
}