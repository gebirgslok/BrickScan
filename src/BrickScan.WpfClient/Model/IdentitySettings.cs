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

using System.Configuration;

namespace BrickScan.WpfClient.Model
{
    internal static class IdentitySettings
    {
        private static readonly string _tenant = ConfigurationManager.AppSettings["Tenant"];
        private static readonly string _azureAdB2CHostname = ConfigurationManager.AppSettings["AzureAdB2CHostname"];
        private static readonly string _authorityBase = $"https://{_azureAdB2CHostname}/tfp/{_tenant}/";
        private static readonly string _policyEditProfile = ConfigurationManager.AppSettings["PolicyEditProfile"];
        private static readonly string _policyResetPassword = ConfigurationManager.AppSettings["PolicyResetPassword"];

        public static readonly string PolicySignUpSignIn = ConfigurationManager.AppSettings["PolicySignUpSignIn"];
        public static readonly string AuthoritySignUpSignIn = $"{_authorityBase}{PolicySignUpSignIn}";
        public static readonly string AuthorityEditProfile = $"{_authorityBase}{_policyEditProfile}";
        public static readonly string AuthorityResetPassword = $"{_authorityBase}{_policyResetPassword}";
        public static readonly string RedirectUri = ConfigurationManager.AppSettings["RedirectUri"];
        public static readonly string ClientId = ConfigurationManager.AppSettings["ClientId"];

        //public static readonly string Authority = string.Format(CultureInfo.InvariantCulture, 
        //    "https://login.microsoftonline.com/{0}/v2.0",
        //    _tenant);
    }
}
