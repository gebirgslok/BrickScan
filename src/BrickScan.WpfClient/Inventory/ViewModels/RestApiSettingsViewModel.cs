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

using System.Security;
using BrickScan.WpfClient.Extensions;
using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class RestApiSettingsViewModel : PropertyChangedBase
    {
        private readonly IUserConfiguration _userConfiguration;
        private string? _authorizationSchemeBefore;
        private SecureString? _authorizationParameterBefore;

        public string? AuthorizationScheme
        {
            get => _userConfiguration.RestApiAuthScheme;
            set => _userConfiguration.RestApiAuthScheme = value;
        }

        public SecureString? AuthorizationParameter
        {
            get => _userConfiguration.RestApiAuthParameter;
            set => _userConfiguration.RestApiAuthParameter = value;
        }

        public string? Url
        {
            get => _userConfiguration.RestApiUrl;
            set => _userConfiguration.RestApiUrl = value;
        }

        [DependsOn(nameof(AuthorizationParameter), nameof(AuthorizationScheme))]
        public bool HasRestApiAuthorizationUnsavedChanged => !string.Equals(_authorizationSchemeBefore, AuthorizationScheme) ||
                                                             !_authorizationParameterBefore.EqualsTo(AuthorizationParameter);

        public RestApiSettingsViewModel(IUserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;
            UpdateAuthorizationPropertiesBeforeSave();
        }

        private void UpdateAuthorizationPropertiesBeforeSave()
        {
            _authorizationSchemeBefore = _userConfiguration.RestApiAuthScheme;
            _authorizationParameterBefore = _userConfiguration.RestApiAuthParameter;
        }

        public void SaveRestApiAuthorization()
        {
            _userConfiguration.SaveRestApiAuthorization();
            UpdateAuthorizationPropertiesBeforeSave();
            NotifyOfPropertyChange(nameof(HasRestApiAuthorizationUnsavedChanged));
        }
    }
}