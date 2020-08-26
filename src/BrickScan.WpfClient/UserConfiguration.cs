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
using BrickScan.WpfClient.Properties;

namespace BrickScan.WpfClient
{
    internal class UserConfiguration : IUserConfiguration
    {
        private static UserConfiguration? _instance;
        public static UserConfiguration Instance => _instance ??= new UserConfiguration();

        public int SelectedSensitivityLevel
        {
            get => Settings.Default.SelectedSensitivityLevel;
            set
            {
                if (value != Settings.Default.SelectedSensitivityLevel)
                {
                    Settings.Default.SelectedSensitivityLevel = value;
                    Save();
                }
            }
        }

        public int SelectedCameraIndex
        {
            get => Settings.Default.SelectedCameraIndex;
            set
            {
                if (value != Settings.Default.SelectedCameraIndex)
                {
                    Settings.Default.SelectedCameraIndex = value;
                    Save();
                }
            }
        }

        public string ThemeColorScheme
        {
            get => Settings.Default.ThemeColorScheme;
            set
            {
                if (!string.Equals(value, Settings.Default.ThemeColorScheme, StringComparison.InvariantCultureIgnoreCase))
                {
                    Settings.Default.ThemeColorScheme = value;
                    Save();
                }
            }
        }

        public string ThemeBaseColor
        {
            get => Settings.Default.ThemeBaseColor;
            set
            {
                if (!string.Equals(value, Settings.Default.ThemeBaseColor, StringComparison.InvariantCultureIgnoreCase))
                {
                    Settings.Default.ThemeBaseColor = value;
                    Save();
                }
            }
        }

        private UserConfiguration()
        {
        }

        public void Save()
        {
            Settings.Default.Save();
        }
    }
}