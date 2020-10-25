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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using BrickScan.WpfClient.Events;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using PropertyChanged;
using Serilog;
using Stylet;
using Application = System.Windows.Application;

namespace BrickScan.WpfClient.Model
{
    internal class UserSession : PropertyChangedBase, IUserSession
    {
        private static readonly string[] _apiScopes = { "https://brickscan.onmicrosoft.com/api/access_as_user" };

        private readonly ILogger _logger;
        private readonly IPublicClientApplication _publicClientApplication;
        private readonly IEventAggregator _eventAggregator;

        public IdentityUser? CurrentUser { get; private set; }

        [DependsOn(nameof(CurrentUser))]
        public bool IsUserLoggedOn => CurrentUser != null;

        [DependsOn(nameof(CurrentUser))]
        public string? Username => CurrentUser?.Name;

        [DependsOn(nameof(CurrentUser))]
        public string Level => CurrentUser != null ? CurrentUser.Level ?? "user" : "unknown";

        [DependsOn(nameof(CurrentUser))]
        public bool IsTrusted => string.Equals(Level, "trusted_user");

        public UserSession(ILogger logger, 
            IPublicClientApplication publicClientApplication, 
            IEventAggregator eventAggregator)
        {
            _logger = logger;
            _publicClientApplication = publicClientApplication;
            _eventAggregator = eventAggregator;
            TokenCacheHelper.Bind(_publicClientApplication.UserTokenCache);
        }

        private static IntPtr GetMainWindowHandle()
        {
            return new WindowInteropHelper(Application.Current.MainWindow!).Handle;
        }

        private void ProcessAuthenticationResult(AuthenticationResult? authenticationResult)
        {
            try
            {
                CurrentUser = authenticationResult != null ?
                    ParseIdToken(authenticationResult.IdToken) :
                    null;
            }
            catch (Exception exception)
            {
                CurrentUser = null;
                _logger.Error(exception, "Failed to parse {IdToken} from authentication result.", authenticationResult?.IdToken ?? "null");
            }
        }

        private static string Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
            var byteArray = Convert.FromBase64String(s);
            var decoded = Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);
            return decoded;
        }

        private static IdentityUser ParseIdToken(string idToken)
        {
            var payload = idToken.Split('.')[1];
            var payloadJson = Base64UrlDecode(payload);
            return JsonConvert.DeserializeObject<IdentityUser>(payloadJson);
        }

        public async Task LogOnAsync()
        {
            var mainWindowHandle = GetMainWindowHandle();
            AuthenticationResult? authenticationResult = null;

            try
            {
                authenticationResult = await _publicClientApplication.AcquireTokenInteractive(_apiScopes)
                    .WithParentActivityOrWindow(mainWindowHandle)
                    .ExecuteAsync();

            }
            catch (MsalException msalException)
            {
                try
                {
                    if (msalException.ErrorCode == "authentication_canceled")
                    {
                        _logger.Information(msalException, "User log on was cancelled.");
                        return;
                    }

                    if (msalException.ErrorCode == "access_denied" && msalException.Message.StartsWith("AADB2C90118"))
                    {
                        authenticationResult = await _publicClientApplication.AcquireTokenInteractive(_apiScopes)
                            .WithParentActivityOrWindow(mainWindowHandle)
                            .WithPrompt(Prompt.SelectAccount)
                            .WithB2CAuthority(IdentitySettings.AuthorityResetPassword)
                            .ExecuteAsync();
                    }
                    else
                    {
                        _logger.Error(msalException, $"Failed to acquire token (unhandled {nameof(MsalException)} on 1st try).");
                    }
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Failed to acquire token after password reset.");
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to acquire token (unexpected exception).");
            }

            ProcessAuthenticationResult(authenticationResult);
        }

        public async Task TryAutoLogOnAsync()
        {
            try
            {
                var accounts = await _publicClientApplication.GetAccountsAsync(IdentitySettings.PolicySignUpSignIn);

                var authenticationResult = await _publicClientApplication
                    .AcquireTokenSilent(_apiScopes, accounts.FirstOrDefault())
                    .ExecuteAsync();

                ProcessAuthenticationResult(authenticationResult);
            }
            catch (MsalException msalException)
            {
                _logger.Information(msalException, $"Failed to auto-log on user. Received a specific {nameof(MsalException)}.");
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Failed to auto-log-on user. Received an unexpected exception.");
            }
        }

        public async Task LogOffAsync()
        {
            var accounts = (await _publicClientApplication.GetAccountsAsync()).ToArray();

            try
            {
                foreach (var account in accounts)
                {
                    await _publicClientApplication.RemoveAsync(account);
                }
            }
            catch (MsalException msalException)
            {
                _logger.Error(msalException, $"Failed to log off user. Received a specific {nameof(MsalException)}.");
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to log off current user. Received an unexpected exception.");
            }

            ProcessAuthenticationResult(null);
        }

        public async Task EditProfileAsync()
        {
            try
            {
                var authenticationResult = await _publicClientApplication.AcquireTokenInteractive(_apiScopes)
                    .WithParentActivityOrWindow(GetMainWindowHandle())
                    .WithB2CAuthority(IdentitySettings.AuthorityEditProfile)
                    .WithPrompt(Prompt.NoPrompt)
                    .ExecuteAsync(new System.Threading.CancellationToken());

                ProcessAuthenticationResult(authenticationResult);
            }
            catch (Exception exception)
            {
                if (exception is MsalException msalException)
                {
                    if (msalException.ErrorCode == "authentication_cancelled" || msalException.Message.StartsWith("AADB2C90091"))
                    {
                        _logger.Debug("'Edit Profile' cancelled by user.");
                        return;
                    } 
                }

                _logger.Error(exception, "Failed to edit profile of the current user. Received an unexpected exception.");

                var message = new OnDialogMessageBoxRequested(Properties.Resources.EditProfileFailedMessage,
                    Properties.Resources.ErrorOccurredCaption,
                    MessageBoxImage.Error);
                _eventAggregator.PublishOnUIThread(message);
            }
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            var accounts = await _publicClientApplication.GetAccountsAsync(IdentitySettings.PolicySignUpSignIn);
            AuthenticationResult? authenticationResult;
            try
            {
                authenticationResult = await _publicClientApplication
                    .AcquireTokenSilent(_apiScopes, accounts.FirstOrDefault())
                    .ExecuteAsync();

                return authenticationResult.AccessToken;
            }
            catch (MsalException msalException)
            {
                _logger.Warning(msalException, $"Received a {nameof(MsalException)} after trying to acquire an access token silently.");

                try
                {
                    authenticationResult = await _publicClientApplication.AcquireTokenInteractive(_apiScopes)
                        .WithParentActivityOrWindow(GetMainWindowHandle())
                        .ExecuteAsync();

                    return authenticationResult.AccessToken;
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Failed to retrieve an access token interactively, " +
                                             $"after {nameof(_publicClientApplication.AcquireTokenSilent)} failed as well." +
                                             Environment.NewLine +
                                             "Received an unexpected exception.");
                    return null;
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to retrieve an access token silently." +
                                         Environment.NewLine +
                                         "Received an unexpected exception.");
                return null;
            }
        }
    }
}