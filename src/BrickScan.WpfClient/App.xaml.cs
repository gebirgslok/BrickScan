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

using System.ComponentModel;
using System.Windows;
using BrickScan.WpfClient.Properties;
using ControlzEx.Theming;
using Serilog;

namespace BrickScan.WpfClient
{
    public partial class App
    {
        public App()
        {
            Settings.Default.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName == nameof(Settings.Default.ThemeBaseColor) ||
                    args.PropertyName == nameof(Settings.Default.ThemeColorScheme))
                {
                    ChangeTheme();
                }
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            SettingsHelper.RestoreSettings();
            ChangeTheme();
            base.OnStartup(e);
        }

        private void ChangeTheme()
        {
            var themeName = $"{Settings.Default.ThemeBaseColor}.{Settings.Default.ThemeColorScheme}";
            Log.ForContext<App>().Information("Changing theme to {ThemeName}.", themeName);
            ThemeManager.Current.ChangeTheme(this, themeName);
        }
    }
}
