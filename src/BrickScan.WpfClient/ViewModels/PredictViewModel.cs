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
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Extensions;
using BrickScan.WpfClient.Model;
using OpenCvSharp;
using Serilog;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    public class PredictViewModel : CameraCaptureBaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger _logger;

        public PredictViewModel(CameraSetupViewModel cameraSetupViewModel, 
            IVideoCapture videoCapture, 
            IEventAggregator eventAggregator, 
            ILogger logger) : base(videoCapture, cameraSetupViewModel)
        {
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void Predict()
        {
            var rect = Rectangle.ToRect();
            try
            {
                var imageSegment = new Mat(Frame!, rect);
                _eventAggregator.PublishOnUIThread(new OnPredictionRequested(imageSegment));
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to publish new prediction request " +
                                         "(frame = {Frame}, rect = {Rect}). Message {Message",
                    Frame?.ToString() ?? "null", rect.ToString());
            }
        }
    }
}
