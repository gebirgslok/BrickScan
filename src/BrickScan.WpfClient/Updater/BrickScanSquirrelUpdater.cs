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
using System.Threading.Tasks;
using BrickScan.Library.Core.Extensions;
using Serilog;
using Squirrel;

namespace BrickScan.WpfClient.Updater
{
    internal class BrickScanSquirrelUpdater : IBrickScanUpdater
    {
        private readonly ILogger _logger;
        private readonly Func<IUpdateManager> _updateManagerFactory;
        private readonly IUserSettingsHelper _userSettingsHelper;

        public BrickScanSquirrelUpdater(Func<IUpdateManager> updateManagerFactory, 
            ILogger logger, 
            IUserSettingsHelper userSettingsHelper)
        {
            _updateManagerFactory = updateManagerFactory;           
            _logger = logger;
            _userSettingsHelper = userSettingsHelper;
        }

        public async Task TryUpdateApplicationAsync(Action<string> messagesCallback, 
            Action clearMessageCallback, 
            Func<Task> confirmRestartTaskFactory, 
            Action restartAppCallback)
        {
            try
            {
                messagesCallback.Invoke(Properties.Resources.CheckingForUpdates);
                
                using (var manager = _updateManagerFactory.Invoke())
                {
                    var updateInfo = await manager.CheckForUpdate(true);
                    clearMessageCallback.Invoke();

                    if (updateInfo == null)
                    {
                        _logger.Warning($"Received no update info (Null) from {nameof(IUpdateManager.CheckForUpdate)} call.");
                        return;
                    }

                    if (!updateInfo.ReleasesToApply.Any())
                    {
                        _logger.Information("No releases to apply.");
                        return;
                    }

                    _logger.Information("Received update info: {CurrentlyInstalledVersion}, {FutureEntry}, {@ReleasesToApply}.",
                        updateInfo.CurrentlyInstalledVersion?.EntryAsString.ToStringOrNullOrEmpty(),
                        updateInfo.FutureReleaseEntry?.EntryAsString.ToStringOrNullOrEmpty(),
                        updateInfo.ReleasesToApply.Select(x => x.EntryAsString));

                    messagesCallback.Invoke(Properties.Resources.DownloadingNewRelease);
                    await manager.DownloadReleases(updateInfo.ReleasesToApply);

                    messagesCallback.Invoke(Properties.Resources.ApplyingUpdate);

                    _userSettingsHelper.Backup();
                    await manager.ApplyReleases(updateInfo);
                }

                clearMessageCallback.Invoke();
                await confirmRestartTaskFactory.Invoke();

                restartAppCallback.Invoke();
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to update the application, received {ExceptionMessage}.",
                    exception.Message);
            }
            finally
            {
                clearMessageCallback.Invoke();
            }
        }
    }
}