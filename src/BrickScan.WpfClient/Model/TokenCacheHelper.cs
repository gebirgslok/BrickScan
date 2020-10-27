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
using System.IO;
using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace BrickScan.WpfClient.Model
{
    static class TokenCacheHelper
    {
        private static readonly string _cacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BrickScan",
            $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.msalcache.bin");

        private static readonly object _fileLock = new object();

        private static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (_fileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(_cacheFilePath)
                    ? ProtectedData.Unprotect(File.ReadAllBytes(_cacheFilePath),
                        null,
                        DataProtectionScope.CurrentUser)
                    : null);
            }
        }

        private static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                lock (_fileLock)
                {
                    var cacheDirectory = Path.GetDirectoryName(_cacheFilePath);
                    Directory.CreateDirectory(cacheDirectory!);

                    File.WriteAllBytes(_cacheFilePath,
                        ProtectedData.Protect(args.TokenCache.SerializeMsalV3(),
                            null,
                            DataProtectionScope.CurrentUser));
                }
            }
        }

        internal static void Bind(ITokenCache tokenCache)
        {
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
        }
    }
}
