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
using System.Drawing;
using System.Threading;
using System.Timers;
using BrickScan.WpfClient.Properties;
using OpenCvSharp;
using Timer = System.Timers.Timer;

namespace BrickScan.WpfClient.Model
{
    internal class OpenCvSharpVideoCapture : IVideoCapture
    {
        public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;

        private readonly IRoiDetector _roiDetector;
        private VideoCapture? _videoCapture;
        private Timer? _timer;
        private long _frameCount;

        public OpenCvSharpVideoCapture(IRoiDetector roiDetector)
        {
            _roiDetector = roiDetector;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var mat = new Mat();
                if (_videoCapture!.Read(mat))
                {
                    var rectangle = _roiDetector.Detect(mat, Settings.Default.SelectedSensitivityLevel);
                    OnFrameCaptured(mat, rectangle);
                }
            }
            catch (Exception ex)
            {
                //todo: do some useful logging
                Debug.WriteLine(ex.Message);
            }
        }

        private void OnFrameCaptured(Mat frame, Rectangle rectangle)
        {
            Interlocked.Increment(ref _frameCount);
            FrameCaptured?.Invoke(this, new FrameCapturedEventArgs(frame, _frameCount, rectangle));
        }

        public bool Open(int cameraIndex)
        {
            try
            {
                _videoCapture = VideoCapture.FromCamera(cameraIndex);
                _timer = new Timer(1000.0 / 30); //30 FPS.
                _timer.Start();
                _frameCount = 0;
                _timer.Elapsed += OnTimerElapsed;
                return true;
            }
            catch (Exception e)
            {
                if (_timer != null)
                {
                    _timer.Elapsed -= OnTimerElapsed;
                    _timer.Dispose();
                }

                _videoCapture?.Dispose();

                //todo: add logging
                return false;
            }
        }

        public void Close()
        {
            _timer?.Dispose();
            _videoCapture?.Dispose();
            _frameCount = 0;
        }
    }
}
