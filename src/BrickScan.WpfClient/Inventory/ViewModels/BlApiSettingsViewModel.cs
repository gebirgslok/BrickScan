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

using PropertyChanged;
using Stylet;

namespace BrickScan.WpfClient.Inventory.ViewModels
{
    public class BlApiSettingsViewModel : PropertyChangedBase
    {
        private readonly IUserConfiguration _userConfiguration;
        private string? _tokenValueBeforeSave;
        private string? _tokenSecretBeforeSave;
        private string? _consumerKeyBeforeSave;
        private string? _consumerSecretBeforeSave;

        public string? BricklinkTokenValue
        {
            get => _userConfiguration.BricklinkTokenValue;
            set => _userConfiguration.BricklinkTokenValue = value;
        }

        public string? BricklinkTokenSecret
        {
            get => _userConfiguration.BricklinkTokenSecret;
            set => _userConfiguration.BricklinkTokenSecret = value;
        }

        public string? BricklinkConsumerKey
        {
            get => _userConfiguration.BricklinkConsumerKey;
            set => _userConfiguration.BricklinkConsumerKey = value;
        }

        public string? BricklinkConsumerSecret
        {
            get => _userConfiguration.BricklinkConsumerSecret;
            set => _userConfiguration.BricklinkConsumerSecret = value;
        }

        [DependsOn(nameof(BricklinkTokenValue), nameof(BricklinkTokenSecret), nameof(BricklinkConsumerKey), nameof(BricklinkConsumerSecret))]
        public bool HasUnsavedBlTokenProperties => !string.Equals(_tokenValueBeforeSave, BricklinkTokenValue) ||
                                                   !string.Equals(_tokenSecretBeforeSave, BricklinkTokenSecret) ||
                                                   !string.Equals(_consumerKeyBeforeSave, BricklinkConsumerKey) ||
                                                   !string.Equals(_consumerSecretBeforeSave, BricklinkConsumerSecret);

        public Condition Condition
        {
            get => _userConfiguration.SelectedBricklinkCondition;
            set
            {
                var index = (int)value;
                if (index != _userConfiguration.SelectedBricklinkConditionIndex)
                {
                    _userConfiguration.SelectedBricklinkConditionIndex = index;
                }
            }
        }

        public decimal BlPriceFixingF
        {
            get => _userConfiguration.BlPriceFixingF;
            set => _userConfiguration.BlPriceFixingF = value;
        }

        public decimal BlPriceFixingC
        {
            get => _userConfiguration.BlPriceFixingC;
            set => _userConfiguration.BlPriceFixingC = value;
        }

        public BlApiSettingsViewModel(IUserConfiguration userConfiguration)
        {
            _userConfiguration = userConfiguration;           
            UpdateTokenPropertiesBeforeSave();
        }

        private void UpdateTokenPropertiesBeforeSave()
        {
            _tokenValueBeforeSave = _userConfiguration.BricklinkTokenValue;
            _tokenSecretBeforeSave = _userConfiguration.BricklinkTokenSecret;
            _consumerKeyBeforeSave = _userConfiguration.BricklinkConsumerKey;
            _consumerSecretBeforeSave = _userConfiguration.BricklinkConsumerSecret;
        }

        public void SaveBricklinkCredentials()
        {
            _userConfiguration.SaveBricklinkCredentials();
            UpdateTokenPropertiesBeforeSave();
            NotifyOfPropertyChange(nameof(HasUnsavedBlTokenProperties));
        }
    }
}