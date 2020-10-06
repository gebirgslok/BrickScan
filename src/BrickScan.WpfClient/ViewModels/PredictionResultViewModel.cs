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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BrickScan.Library.Core.Dto;
using BrickScan.WpfClient.Events;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public class PredictionResultViewModel : PropertyChangedBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IPredictedClassViewModelFactory _predictedClassViewModelFactory;
        private readonly HttpClient _httpClient;

        public IEnumerable<PredictedClassViewModelBase>? PredictedClassViewModels { get; set; }

        public BitmapSource ImageSource { get; }

        public NotifyTask<List<PredictedDatasetClassDto>> InitializationNotifier { get; }

        public string ImageSizeString => $"{ImageSource.PixelWidth}×{ImageSource.PixelHeight}";

        public PredictionResultViewModel(Mat imageSection, 
            Func<Task<List<PredictedDatasetClassDto>>, List<PredictedDatasetClassDto>, NotifyTask<List<PredictedDatasetClassDto>>> notifyTaskFactory, 
            IEventAggregator eventAggregator, 
            HttpClient httpClient, 
            IPredictedClassViewModelFactory predictedClassViewModelFactory)
        {
            _eventAggregator = eventAggregator;
            _httpClient = httpClient;
            _predictedClassViewModelFactory = predictedClassViewModelFactory;
            ImageSource = imageSection.ToBitmapSource();
            var task = StartPredictionAsync(imageSection);
            InitializationNotifier = notifyTaskFactory.Invoke(task, new List<PredictedDatasetClassDto>());

            InitializationNotifier.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args)
            {
                //TODO ERROR handling here.

                if (args.PropertyName == nameof(InitializationNotifier.IsCompleted))
                {
                    PredictedClassViewModels = BuildViewModels(InitializationNotifier.Result);
                }
            };
        }

        private IEnumerable<PredictedClassViewModelBase> BuildViewModels(List<PredictedDatasetClassDto> predictedClases)
        {
            return predictedClases.Select(x => _predictedClassViewModelFactory.Create(x));
        }

        private async Task<List<PredictedDatasetClassDto>> StartPredictionAsync(Mat imageSection)
        {
            //TODO: make code nicer, error handling for response.
            var formData = new MultipartFormDataContent();
            var content = new ByteArrayContent(imageSection.ToBytes());
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            formData.Add(content, "image", "prediction-image.png");
            var response = await _httpClient.PostAsync("prediction", formData);
            var responseString = await response.Content.ReadAsStringAsync();
            var jsonObj = JObject.Parse(responseString);
            var jsonData = jsonObj["data"];

            if (jsonData == null)
            {
                throw new InvalidOperationException(
                    "Received JSON response (for request POST /images) did not contain a 'data' object.");
            }

            var predictedClasses = jsonData.ToObject<List<PredictedDatasetClassDto>>() ?? 
                                   new List<PredictedDatasetClassDto>();
            return predictedClasses;
        }

        public void NavigateBack()
        {
            _eventAggregator.PublishOnUIThread(new OnPredictionResultCloseRequested());
        }
    }
}