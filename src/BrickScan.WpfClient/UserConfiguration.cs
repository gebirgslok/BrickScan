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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using BricklinkSharp.Client;
using BrickScan.Library.Core;
using BrickScan.WpfClient.Extensions;
using BrickScan.WpfClient.Inventory;
using BrickScan.WpfClient.Properties;
using Serilog;
using Condition = BrickScan.WpfClient.Inventory.Condition;

namespace BrickScan.WpfClient
{
    internal class UserConfiguration : IUserConfiguration
    {
        private static readonly string _bricklinkCredentialsFilename = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrickScan",
            $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.blcredentials.bin");

        private static readonly string _restApiAuthFilename = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrickScan",
            $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.restApiAuthcredentials.bin");

        private readonly ILogger _logger;

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

        public int SelectedInventoryServiceTypeIndex
        {
            get => Settings.Default.SelectedInventoryServiceType;
            set
            {
                if (value != Settings.Default.SelectedInventoryServiceType)
                {
                    Settings.Default.SelectedInventoryServiceType = value;
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

        public InventoryServiceType SelectedInventoryServiceType =>
            (InventoryServiceType)SelectedInventoryServiceTypeIndex;

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

        public string SelectedCultureKey
        {
            get => Settings.Default.SelectedCultureKey;
            set
            {
                if (!string.Equals(value, Settings.Default.SelectedCultureKey, StringComparison.InvariantCultureIgnoreCase))
                {
                    Settings.Default.SelectedCultureKey = value;
                    Save();
                }
            }
        }

        public string? BricklinkTokenValue
        {
            get => BricklinkClientConfiguration.Instance.TokenValue; 
            set => BricklinkClientConfiguration.Instance.TokenValue = value;
        }

        public string? BricklinkTokenSecret
        {
            get => BricklinkClientConfiguration.Instance.TokenSecret; 
            set => BricklinkClientConfiguration.Instance.TokenSecret = value;
        }

        public string? BricklinkConsumerKey
        {
            get => BricklinkClientConfiguration.Instance.ConsumerKey; 
            set => BricklinkClientConfiguration.Instance.ConsumerKey = value;
        }

        public string? BricklinkConsumerSecret
        {
            get => BricklinkClientConfiguration.Instance.ConsumerSecret; 
            set => BricklinkClientConfiguration.Instance.ConsumerSecret = value;
        }

        public int SelectedInventoryConditionIndex
        {
            get => Settings.Default.SelectedInventoryConditionIndex;
            set
            {
                if (value != Settings.Default.SelectedInventoryConditionIndex)
                {
                    Settings.Default.SelectedInventoryConditionIndex = value;
                    Save();
                }
            }
        }

        public Condition SelectedInventoryCondition=> (Condition)SelectedInventoryConditionIndex;

        public int SelectedPriceFixingBaseMethodIndex
        {
            get => Settings.Default.SelectedPriceFixingBaseMethodIndex;
            set
            {
                if (value != Settings.Default.SelectedPriceFixingBaseMethodIndex)
                {
                    Settings.Default.SelectedPriceFixingBaseMethodIndex = value;
                    Save();
                }
            }
        }

        public PriceFixingBaseMethod SelectedPriceFixingBaseMethod => (PriceFixingBaseMethod)SelectedPriceFixingBaseMethodIndex;

        public decimal BlPriceFixingC
        {
            get => Settings.Default.BlPriceFixingC;
            set
            {
                if (value != Settings.Default.BlPriceFixingC)
                {
                    Settings.Default.BlPriceFixingC = value;
                    Save();
                }
            }
        }

        public decimal BlPriceFixingF
        {
            get => Settings.Default.BlPriceFixingF;
            set
            {
                if (value != Settings.Default.BlPriceFixingF)
                {
                    Settings.Default.BlPriceFixingF = value;
                    Save();
                }
            }
        }

        public string? RestApiUrl
        {
            get => Settings.Default.RestApiUrl;
            set
            {
                if (value != Settings.Default.RestApiUrl)
                {
                    Settings.Default.RestApiUrl = value;
                    Save();
                }
            }
        }

        public string? RestApiAuthScheme { get; set; }
        public SecureString? RestApiAuthParameter { get; set; }

        public UserConfiguration(ILogger logger)
        {
            _logger = logger;
            ReadBricklinkCredentialsIfFileExists();
            ReadRestApiAuthorizationIfFileExists();
        }

        private static string Unprotect(byte[] bytes, byte[] entropy)
        {
            byte[] plaintext = ProtectedData.Unprotect(bytes, entropy,
                DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(plaintext);
        }

        private static byte[] Protect(string? s, byte[] entropy)
        {
            if (s == null)
            {
                return new byte[0];
            }

            var plainBytes = Encoding.UTF8.GetBytes(s);
            var cipherBytes = ProtectedData.Protect(plainBytes, entropy, DataProtectionScope.CurrentUser);
            return cipherBytes;
        }

        private void ReadBricklinkCredentialsIfFileExists()
        {
            if (!File.Exists(_bricklinkCredentialsFilename))
            {
                return;
            }

            try
            {
                var binaryData = FileHelper
                    .ReadByteArrays(_bricklinkCredentialsFilename)
                    .ToArray();

                var entropy = binaryData[0];
                BricklinkTokenValue = Unprotect(binaryData[1], entropy);
                BricklinkTokenSecret = Unprotect(binaryData[2], entropy);
                BricklinkConsumerKey = Unprotect(binaryData[3], entropy);
                BricklinkConsumerSecret = Unprotect(binaryData[4], entropy);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to read BL credentials from {File}.", _bricklinkCredentialsFilename);
            }
        }

        private void ReadRestApiAuthorizationIfFileExists()
        {
            if (!File.Exists(_restApiAuthFilename))
            {
                return;
            }

            try
            {
                var binaryData = FileHelper
                    .ReadByteArrays(_restApiAuthFilename)
                    .ToArray();

                var entropy = binaryData[0];
                RestApiAuthScheme = Unprotect(binaryData[1], entropy);
                RestApiAuthParameter = Unprotect(binaryData[2], entropy).ToSecuredString();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to read REST API authorization from {File}.", _restApiAuthFilename);
            }
        }

        public void Save()
        {
            Settings.Default.Save();
        }

        public void SaveBricklinkCredentials()
        {
            byte[] entropy = new byte[20];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(entropy);
            }

            var binaryData = new List<byte[]>
            {
                entropy,
                Protect(BricklinkTokenValue, entropy),
                Protect(BricklinkTokenSecret, entropy),
                Protect(BricklinkConsumerKey, entropy),
                Protect(BricklinkConsumerSecret, entropy),
            };

            FileHelper.WriteByteArrays(_bricklinkCredentialsFilename, binaryData);
        }

        public void SaveRestApiAuthorization()
        {
            byte[] entropy = new byte[20];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(entropy);
            }

            var binaryData = new List<byte[]>
            {
                entropy,
                Protect(RestApiAuthScheme, entropy),
                Protect(RestApiAuthParameter.ToUnsecuredString(), entropy)
            };

            FileHelper.WriteByteArrays(_restApiAuthFilename, binaryData);
        }
    }
}