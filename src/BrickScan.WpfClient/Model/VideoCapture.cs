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
using System.Drawing;
using System.Threading;
using System.Timers;
using OpenCvSharp;
using Serilog;
using Timer = System.Timers.Timer;

namespace BrickScan.WpfClient.Model
{
    internal class VideoCapture : IVideoCapture
    {
        public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;

        private readonly IRoiDetector _roiDetector;
        private readonly ILogger _logger;
        private OpenCvSharp.VideoCapture? _videoCapture;
        private Timer? _timer;
        private long _frameCount;
        private bool _loggedExceptionForSession;

        public VideoCapture(IRoiDetector roiDetector, ILogger logger)
        {
            _roiDetector = roiDetector;
            _logger = logger;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var mat = new Mat();
                if (_videoCapture!.Read(mat))
                {
                    var rectangle = _roiDetector.Detect(mat);
                    OnFrameCaptured(mat, rectangle);
                }
            }
            catch (Exception ex)
            {
                if (!_loggedExceptionForSession)
                {
                    _logger.Error(ex, "An exception occurred while processing the next frame (frame# {FrameCount}). {Message}", 
                        _frameCount, 
                        ex.Message);

                    _loggedExceptionForSession = true;
                }
            }
        }

        private void OnFrameCaptured(Mat frame, Rectangle rectangle)
        {
            Interlocked.Increment(ref _frameCount);
            _logger.Verbose("Captured frame# {FrameCount}, detected rectangle = {Rectangle}.", _frameCount, rectangle.ToString());
            FrameCaptured?.Invoke(this, new FrameCapturedEventArgs(frame, _frameCount, rectangle));
        }

        public bool Open(int cameraIndex)
        {
            try
            {
                _videoCapture = OpenCvSharp.VideoCapture.FromCamera(cameraIndex);
                _logger.Information("Successfully opened camera {CameraIndex}.", cameraIndex);
                const double timerInterval = 1000.0 / 30;
                _timer = new Timer(timerInterval); //30 FPS.
                _frameCount = 0;
                _loggedExceptionForSession = false;
                _timer.Start();

                _logger.Information("Set up capture session: timerInterval = {TimerInterval}, " +
                                    "_frameCount = {FrameCount}, _loggedExceptionForSession = {LoggedExceptionForSession}.",
                    timerInterval, _frameCount, _loggedExceptionForSession);

                _timer.Elapsed += OnTimerElapsed;
                return true;
            }
            catch (Exception exception)
            {
                if (_timer != null)
                {
                    _timer.Elapsed -= OnTimerElapsed;
                    _timer.Dispose();
                }

                _videoCapture?.Dispose();
                _logger.Error(exception, "Failed to open video capture from camera {CameraIndex}. {Message}", cameraIndex, exception.Message);
                return false;
            }
        }

        public void Close()
        {
            try
            {
                _frameCount = 0;
                _loggedExceptionForSession = false;
                _timer?.Dispose();
                _videoCapture?.Dispose();
                _videoCapture = null;
                _logger.Information("Successfully closed (disposed) camera.");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to close (dispose) video capture. {Message}", exception.Message);
            }
        }
    }
}
