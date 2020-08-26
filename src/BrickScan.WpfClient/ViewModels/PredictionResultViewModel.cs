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
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Media;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using Stylet;

// ReSharper disable once UnusedAutoPropertyAccessor.Global
// ReSharper disable once MemberCanBePrivate.Global
namespace BrickScan.WpfClient.ViewModels
{
    internal class PredictionResultViewModel : PropertyChangedBase
    {
        public string Test { get; set; }

        public ImageSource ImageSource { get; }

        public NotifyTask<string> InitializationNotifier { get; }

        public PredictionResultViewModel(Mat imageSection, Func<Task<string>, string, NotifyTask<string>> notifyTaskFactory)
        {
            ImageSource = imageSection.ToBitmapSource();
            var task = StartPredictionAsync(imageSection).ContinueWith(s => s.Result + "foo");
            InitializationNotifier = notifyTaskFactory.Invoke(task, "foobar");
            InitializationNotifier.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName == nameof(InitializationNotifier.IsCompleted))
                {
                    Test = InitializationNotifier.Result;
                }
            };
        }

        private async Task<string> StartPredictionAsync(Mat imageSection)
        {
            await Task.Delay(5000);
            return "hello world";
        }
    }
}