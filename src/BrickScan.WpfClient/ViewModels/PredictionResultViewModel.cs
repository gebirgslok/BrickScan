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
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BrickScan.Library.Core.Dto;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Model;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Serilog;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public class PredictionResultViewModel : PropertyChangedBase, IHandle<OnImageConfirmed>
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IPredictedClassViewModelFactory _predictedClassViewModelFactory;
        private readonly IBrickScanApiClient _apiClient;
        private readonly ILogger _logger;

        public BindableCollection<PredictedClassViewModelBase>? PredictedClassViewModels { get; set; }

        public BitmapSource ImageSource { get; }

        public NotifyTask<PredictionResult> InitializationNotifier { get; }

        public string ImageSizeString => $"{ImageSource.PixelWidth}×{ImageSource.PixelHeight}";

        public PredictionResultViewModel(Mat imageSection,
            Func<Task<PredictionResult>, PredictionResult, NotifyTask<PredictionResult>> notifyTaskFactory,
            IEventAggregator eventAggregator,
            IPredictedClassViewModelFactory predictedClassViewModelFactory, 
            ILogger logger,
            IBrickScanApiClient apiClient)
        {
            _eventAggregator = eventAggregator;
            _predictedClassViewModelFactory = predictedClassViewModelFactory;
            _logger = logger;
            _apiClient = apiClient;
            ImageSource = imageSection.ToBitmapSource();

            var task = StartPredictionAsync(imageSection);
            InitializationNotifier = notifyTaskFactory.Invoke(task, PredictionResult.UnexpectedError);

            InitializationNotifier.PropertyChanged += delegate (object sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName == nameof(InitializationNotifier.IsSuccessfullyCompleted))
                {
                    var viewModels = BuildViewModels(InitializationNotifier.Result.PredictedDatasetClasses);
                    PredictedClassViewModels = new BindableCollection<PredictedClassViewModelBase>(viewModels);
                }
            };
        }

        private IEnumerable<PredictedClassViewModelBase> BuildViewModels(List<PredictedDatasetClassDto>? predictedClases)
        {
            return predictedClases?.Select(x => _predictedClassViewModelFactory.Create(x)) ?? 
                   new List<PredictedClassViewModelBase>();
        }

        private Mat ResizeIfTooLarge(Mat input)
        {
            var maxWidthOrHeight = Math.Max(input.Width, input.Width);
            var t = AppConfig.MaxNonDisplayImageWidthOrHeight;

            if (maxWidthOrHeight > t)
            {
                var newWidth = (int)(1.0 * input.Width * t / maxWidthOrHeight);
                var newHeight = (int)(1.0 * input.Height * t / maxWidthOrHeight);

                _logger.Information("Input image {Size} was tool large, resized to {NewWidth} x {NewHeight}.", 
                    input.Size(), 
                    newWidth, 
                    newHeight);

                return input.Resize(new Size(newWidth, newHeight));
            }

            return input;
        }

        private async Task<PredictionResult> StartPredictionAsync(Mat imageSection)
        {
            imageSection = ResizeIfTooLarge(imageSection);
            var result = await _apiClient.PredictAsync(imageSection.ToBytes());

            if (!result.Sucess)
            {
                throw new HttpRequestException(result.ErrorMessage);
            }

            return result;
        }

        public void NavigateBack()
        {
            _eventAggregator.PublishOnUIThread(new OnPredictionResultCloseRequested());
        }

        public void Handle(OnImageConfirmed message)
        {
            if (PredictedClassViewModels != null)
            {
                for (var i = PredictedClassViewModels.Count - 1; i >= 0; i--)
                {
                    if (PredictedClassViewModels[i].PredictedClassId != message.PredictedClassId)
                    {
                        PredictedClassViewModels.RemoveAt(i);
                    }
                }
            }
        }
    }
}