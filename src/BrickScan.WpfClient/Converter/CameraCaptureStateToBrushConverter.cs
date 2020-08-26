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
using System.Windows.Data;
using System.Windows.Media;
using BrickScan.WpfClient.Model;

namespace BrickScan.WpfClient.Converter
{
    [ValueConversion(typeof(CameraCaptureState), typeof(Brush))]
    internal class CameraCaptureStateToBrushConverter : IValueConverter
    {
        private static readonly Brush _idle = new SolidColorBrush(Colors.LightGray);
        private static readonly Brush _capturing = new SolidColorBrush(Colors.LimeGreen);
        private static readonly Brush _connectingOrDisconnecting = new SolidColorBrush(Colors.Yellow);
        private static readonly Brush _error = new SolidColorBrush(Colors.OrangeRed);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (CameraCaptureState)value;

            return state switch
            {
                CameraCaptureState.Idle => _idle,
                CameraCaptureState.Connecting => _connectingOrDisconnecting,
                CameraCaptureState.Disconnecting => _connectingOrDisconnecting,
                CameraCaptureState.Capturing => _capturing,
                CameraCaptureState.Error => _error,
                _ => throw new ArgumentOutOfRangeException(nameof(state))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"{nameof(CameraCaptureStateToBrushConverter)} " +
                                              "only supports one-way conversion.");
        }
    }
}
