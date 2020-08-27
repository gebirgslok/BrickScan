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

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using ControlzEx.Theming;
using Stylet;
// ReSharper disable ClassNeverInstantiated.Global

namespace BrickScan.WpfClient.ViewModels
{
    internal class SettingsViewModel : PropertyChangedBase
    {
        private static readonly IReadOnlyCollection<LanguageOption> _availableLanguageOptions = GetAvailableLanguageOptions();
        private readonly IUserConfiguration _userConfiguration;

        public IReadOnlyCollection<string> AvailableThemeColorSchemes => ThemeManager.Current.ColorSchemes;

        public IReadOnlyCollection<string> AvailableThemeBaseColors => ThemeManager.Current.BaseColors;

        public IReadOnlyCollection<LanguageOption> AvailableLanguageOptions => _availableLanguageOptions;

        private LanguageOption _selectedLanguageOption = null!;
        public LanguageOption SelectedLanguageOption
        {
            get { return _selectedLanguageOption; }
            set
            {
                if (!ReferenceEquals(value, _selectedLanguageOption))
                {
                    _selectedLanguageOption = value;
                    _userConfiguration.SelectedCultureKey = _selectedLanguageOption.CultureKey;
                    NotifyOfPropertyChange(nameof(SelectedLanguageOption));
                }
            }
        }

        public string SelectedThemeBaseColor
        {
            get => _userConfiguration.ThemeBaseColor;
            set => _userConfiguration.ThemeBaseColor = value;
        }

        public string SelectedThemeColorScheme
        {
            get => _userConfiguration.ThemeColorScheme;
            set => _userConfiguration.ThemeColorScheme = value;
        }

        public SettingsViewModel(IUserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;
            SelectedLanguageOption =
                AvailableLanguageOptions.First(option => option.CultureKey == _userConfiguration.SelectedCultureKey);
        }

        private static IReadOnlyCollection<LanguageOption> GetAvailableLanguageOptions()
        {
            var settingCollection = (NameValueCollection)ConfigurationManager.GetSection("languageOptions");

            var options = settingCollection.AllKeys
                .Select(key => new LanguageOption(key, settingCollection[key]))
                .ToArray();

            return options;
        }
    }
}
