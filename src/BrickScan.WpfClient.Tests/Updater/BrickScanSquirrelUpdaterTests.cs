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
using System.Collections.Generic;
using System.Threading.Tasks;
using BrickScan.WpfClient.Updater;
using FakeItEasy;
using Serilog;
using Squirrel;
using Xunit;

namespace BrickScan.WpfClient.Tests.Updater
{
    public class BrickScanSquirrelUpdaterTests
    {
        class DummyUpdateInfo : UpdateInfo
        {
            public DummyUpdateInfo(ReleaseEntry currentlyInstalledVersion, IEnumerable<ReleaseEntry> releasesToApply, string packageDirectory) : base(currentlyInstalledVersion, releasesToApply, packageDirectory)
            {
            }
        }

        class DummyReleaseEntry : ReleaseEntry
        {
            public DummyReleaseEntry(string sha1, string filename) : base(sha1, filename, 42, false, null, null, null)
            {
            }
        }

        private static UpdateInfo CreateUpdateInfoWithoutNewerReleases()
        {
            return new DummyUpdateInfo(new DummyReleaseEntry("1234", "foo.1.0.0.nupkg"),
                new List<ReleaseEntry>(),
                "\\path\\to\\my\\non\\existing\\packages");
        }

        private static UpdateInfo CreateUpdateInfoWithNewerReleases()
        {
            return new DummyUpdateInfo(new DummyReleaseEntry("1234", "foo.1.0.0.nupkg"), new List<ReleaseEntry>
                {
                    new DummyReleaseEntry("12345", "foo.1.0.0.nupkg")
                },
                "\\path\\to\\my\\non\\existing\\packages");
        }

        [Fact]
        public async Task TryUpdateApplicationAsync_EnsureDependencyCallOrder_WhenNewerReleasesAvailable()
        {
            var updateManager = A.Fake<IUpdateManager>();
            var updateInfo = CreateUpdateInfoWithNewerReleases();
            A.CallTo(() => updateManager.CheckForUpdate(true, null, UpdaterIntention.Update))
                .Returns(Task.FromResult(updateInfo));

            var updateManagerFactory = A.Fake<Func<IUpdateManager>>();
            A.CallTo(() => updateManagerFactory.Invoke()).Returns(updateManager);
            var userSettingsHelper = A.Fake<IUserSettingsHelper>();

            var updater = new BrickScanSquirrelUpdater(updateManagerFactory, A.Dummy<ILogger>(), userSettingsHelper);

            var messageCallback = A.Fake<Action<string>>();
            var clearMessagesCallback = A.Fake<Action>();
            var confirmRestartTaskFactory = A.Fake<Func<Version, Task>>();
            var restartAppCallback = A.Fake<Action>();

            await updater.TryUpdateApplicationAsync(messageCallback, clearMessagesCallback, confirmRestartTaskFactory, restartAppCallback);

            A.CallTo(() => messageCallback.Invoke(Properties.Resources.CheckingForUpdates)).MustHaveHappenedOnceExactly()
                .Then(A.CallTo(() => updateManagerFactory.Invoke()).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => updateManager.CheckForUpdate(true, null, UpdaterIntention.Update)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => messageCallback.Invoke(Properties.Resources.DownloadingNewRelease)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => updateManager.DownloadReleases(updateInfo.ReleasesToApply, null)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => messageCallback.Invoke(Properties.Resources.ApplyingUpdate)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => userSettingsHelper.Backup()).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => updateManager.ApplyReleases(updateInfo, null)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => confirmRestartTaskFactory.Invoke(A<Version>._)).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => restartAppCallback.Invoke()).MustHaveHappenedOnceExactly());

            A.CallTo(() => clearMessagesCallback.Invoke()).MustHaveHappenedANumberOfTimesMatching(num => num == 3);
        }

        [Fact]
        public async Task TryUpdateApplicationAsync_EnsureDependencyCallorder_NoNewerReleaseAvailable()
        {
            var updateManager = A.Fake<IUpdateManager>();
            var updateInfo = CreateUpdateInfoWithoutNewerReleases();
            A.CallTo(() => updateManager.CheckForUpdate(true, null, UpdaterIntention.Update))
                .Returns(Task.FromResult(updateInfo));

            var updateManagerFactory = A.Fake<Func<IUpdateManager>>();
            A.CallTo(() => updateManagerFactory.Invoke()).Returns(updateManager);
            var userSettingsHelper = A.Fake<IUserSettingsHelper>();

            var updater = new BrickScanSquirrelUpdater(updateManagerFactory, A.Dummy<ILogger>(), userSettingsHelper);

            var messageCallback = A.Fake<Action<string>>();
            var clearMessagesCallback = A.Fake<Action>();
            var confirmRestartTaskFactory = A.Fake<Func<Version, Task>>();
            var restartAppCallback = A.Fake<Action>();

            await updater.TryUpdateApplicationAsync(messageCallback, 
                clearMessagesCallback, 
                confirmRestartTaskFactory, 
                restartAppCallback);

            A.CallTo(() => messageCallback.Invoke(Properties.Resources.CheckingForUpdates))
                .MustHaveHappenedOnceExactly()
                .Then(A.CallTo(() => updateManagerFactory.Invoke()).MustHaveHappenedOnceExactly())
                .Then(A.CallTo(() => updateManager.CheckForUpdate(true, null, UpdaterIntention.Update)).MustHaveHappenedOnceExactly());

            A.CallTo(() => messageCallback.Invoke(Properties.Resources.DownloadingNewRelease)).MustNotHaveHappened();
            A.CallTo(() => updateManager.DownloadReleases(updateInfo.ReleasesToApply, null))
                .MustNotHaveHappened();
            A.CallTo(() => messageCallback.Invoke(Properties.Resources.ApplyingUpdate))
                .MustNotHaveHappened();
            A.CallTo(() => userSettingsHelper.Backup()).MustNotHaveHappened();
            A.CallTo(() => updateManager.ApplyReleases(updateInfo, null)).MustNotHaveHappened();
            A.CallTo(() => confirmRestartTaskFactory.Invoke(A<Version>._)).MustNotHaveHappened();
            A.CallTo(() => restartAppCallback.Invoke()).MustNotHaveHappened();
            A.CallTo(() => clearMessagesCallback.Invoke()).MustHaveHappenedTwiceExactly();
        }
    }
}
