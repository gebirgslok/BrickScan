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
using System.Linq;
using System.Threading.Tasks;
using BrickScan.WpfClient.Model;
using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using Serilog;
using Squirrel;
using Stylet;

namespace BrickScan.WpfClient.ViewModels
{
    internal class MainViewModel : Screen
    {
        private readonly PredictionConductorViewModel _predictionConductorViewModel;
        private readonly AddPartsConductorViewModel _addPartsConductorViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly ILogger _logger;

        public StatusBarViewModel StatusBarViewModel { get; }

        public HamburgerMenuItemCollection MenuItems => BuildMenuItems();

        public HamburgerMenuItemCollection MenuOptionItems => BuildMenuOptionsItems();

        public HamburgerMenuItem? SelectedItem { get; set; }

        public IUserSession UserSession { get; }

        public MainViewModel(PredictionConductorViewModel predictionConductorViewModel,
            AddPartsConductorViewModel addPartsConductorViewModel,
            SettingsViewModel settingsViewModel,
            StatusBarViewModel statusBarViewModel,
            IUserSession userManager, 
            ILogger logger)
        {
            _predictionConductorViewModel = predictionConductorViewModel;
            _addPartsConductorViewModel = addPartsConductorViewModel;
            _settingsViewModel = settingsViewModel;
            StatusBarViewModel = statusBarViewModel;
            UserSession = userManager;
            _logger = logger;
            SelectedItem = MenuItems[0] as HamburgerMenuIconItem;
        }

        private HamburgerMenuItemCollection BuildMenuItems()
        {
            var collection = new HamburgerMenuItemCollection
            {
                new HamburgerMenuIconItem
                {
                    Icon = new PackIconMaterial{ Kind = PackIconMaterialKind.Home },
                    Label = Properties.Resources.Home.ToUpperInvariant(),
                    Tag = _predictionConductorViewModel
                },
                new HamburgerMenuIconItem
                {
                    Icon = new PackIconMaterial{ Kind = PackIconMaterialKind.CameraPlus },
                    Label = Properties.Resources.AddParts.ToUpperInvariant(),
                    Tag = _addPartsConductorViewModel
                }
            };

            return collection;
        }

        private HamburgerMenuItemCollection BuildMenuOptionsItems()
        {
            var collection = new HamburgerMenuItemCollection
            {
                new HamburgerMenuIconItem
                {
                    Icon = new PackIconMaterial{ Kind = PackIconMaterialKind.Cog },
                    Label = Properties.Resources.Settings.ToUpperInvariant(),
                    Tag = _settingsViewModel
                }
            };

            return collection;
        }

        private async Task UpdateApplication()
        {
            try
            {
                var url = ConfigurationManager.AppSettings["SquirrelReleasesUrl"];
                using var manager = new UpdateManager(url);
                var updateInfo = await manager.CheckForUpdate(true);

                if (updateInfo == null)
                {
                    _logger.Warning($"Received no update info (Null) from {nameof(UpdateManager.CheckForUpdate)} call.");
                    return;
                }

                _logger.Information("Received update info: {CurrentlyInstalledVersion}, {FutureEntry}, {@ReleasesToApply}.", 
                    updateInfo.CurrentlyInstalledVersion.EntryAsString,
                    updateInfo.FutureReleaseEntry.EntryAsString,
                    updateInfo.ReleasesToApply.Select(x => x.EntryAsString));

                await manager.ApplyReleases(updateInfo);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Failed to update the application, received {ExceptionMessage}.", exception.Message);
            }
        }

        public void CloseApplication()
        {
            RequestClose();
        }

        public void LaunchBrickScanOnGithub()
        {
            System.Diagnostics.Process.Start("https://github.com/gebirgslok/BrickScan");
        }

        public void LaunchBrickScanHomepage()
        {

        }

        public async Task LogOnAsync()
        {
            await UserSession.LogOnAsync();
        }

        public async Task LogOffAsync()
        {
            await UserSession.LogOffAsync();
        }

        public async Task OnLoaded()
        {
#if !DEBUG
            await UpdateApplication();
#endif
            await UserSession.TryAutoLogOnAsync();
        }

        public async Task EditProfileAsync()
        {
            await UserSession.EditProfileAsync();
        }
    }
}
