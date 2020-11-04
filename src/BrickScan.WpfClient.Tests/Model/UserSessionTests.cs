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
using System.Threading.Tasks;
using BrickScan.WpfClient.Model;
using FakeItEasy;
using Microsoft.Identity.Client;
using Serilog;
using Stylet;
using Xunit;

namespace BrickScan.WpfClient.Tests.Model
{
    public class UserSessionTests
    {
        private static UserSession Create(ILogger? logger = null,
            IPublicClientApplication? publicClientApplication = null,
            IEventAggregator? eventAggregator = null)
        {
            return new UserSession(logger ?? A.Dummy<ILogger>(),
                publicClientApplication ?? A.Dummy<IPublicClientApplication>(),
                eventAggregator ?? A.Dummy<IEventAggregator>());
        }

        [Fact]
        public async Task GetAccessTokenAsync_AcquireTokenSilentThrowsMsalExecption_ReturnsNull()
        {
            var publicClientApplication = A.Fake<IPublicClientApplication>();
            A.CallTo(() => publicClientApplication.AcquireTokenSilent(A<IEnumerable<string>>._, A<IAccount>._))
                .Throws(() => new MsalException());

            var userSession = Create(publicClientApplication: publicClientApplication);

            var token = await userSession.GetAccessTokenAsync(false);

            Assert.Null(token);
        }
    }
}
