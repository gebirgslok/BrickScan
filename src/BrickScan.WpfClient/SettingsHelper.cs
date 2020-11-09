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
using System.Configuration;
using System.IO;
using BrickScan.WpfClient.Properties;

namespace BrickScan.WpfClient
{
    internal static class SettingsHelper
    {
        private static readonly string _lastConfigFile =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BrickScan",
                "last.config");

        public static void RestoreSettings()
        {
            var destFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            var destDir = Path.GetDirectoryName(destFile)!;
            var sourceFile = _lastConfigFile;

            if (!File.Exists(sourceFile))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(destDir);
            }
            catch
            {
                // ignored
            }

            try
            {
                File.Copy(sourceFile, destFile, true);
            }
            catch
            {
                // ignored
            }

            try
            {
                File.Delete(sourceFile);
            }
            catch
            {
                // ignored
            }

            Settings.Default.Reload();
        }

        public static void BackupSettings()
        {
            var settingsFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;

            try
            {
                File.Copy(settingsFile, _lastConfigFile, true);
            }
            catch
            {
                // ignored
            }
        }
    }
}