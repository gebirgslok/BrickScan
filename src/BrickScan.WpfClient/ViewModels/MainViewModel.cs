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
using System.Windows;
using BrickScan.WpfClient.Events;
using BrickScan.WpfClient.Model;
using BrickScan.WpfClient.Updater;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Squirrel;
using Stylet;
using ILogger = Serilog.ILogger;

namespace BrickScan.WpfClient.ViewModels
{
    internal class MainViewModel : Screen, IHandle<OnDialogMessageBoxRequested>, IDisposable
    {
        private readonly PredictionConductorViewModel _predictionConductorViewModel;
        private readonly AddPartsConductorViewModel _addPartsConductorViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly ILogger _logger;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly IBrickScanUpdater _updater;
        private bool _isDisposed;

        public StatusBarViewModel StatusBarViewModel { get; }

        private HamburgerMenuItemCollection? _menuItems;

        public HamburgerMenuItemCollection MenuItems => _menuItems ??= BuildMenuItems();

        public HamburgerMenuItemCollection MenuOptionItems => BuildMenuOptionsItems();

        public HamburgerMenuItem? SelectedItem { get; set; }

        public IUserSession UserSession { get; }

        public MainViewModel(PredictionConductorViewModel predictionConductorViewModel,
            AddPartsConductorViewModel addPartsConductorViewModel,
            SettingsViewModel settingsViewModel,
            StatusBarViewModel statusBarViewModel,
            IUserSession userManager,
            ILogger logger,
            IDialogCoordinator dialogCoordinator,
            IBrickScanUpdater updater)
        {
            _predictionConductorViewModel = predictionConductorViewModel;
            _addPartsConductorViewModel = addPartsConductorViewModel;
            _settingsViewModel = settingsViewModel;
            StatusBarViewModel = statusBarViewModel;

            UserSession = userManager;
            UserSession.UserChanged += HandleUserChanged;

            _logger = logger;
            _dialogCoordinator = dialogCoordinator;
            _updater = updater;
            SelectedItem = MenuItems[0] as HamburgerMenuIconItem;
        }

        private void HandleUserChanged(object sender, UserChangedEventArgs e)
        {
            var item = MenuItems.FirstOrDefault(x => x.Tag == _addPartsConductorViewModel);

            if (item == null)
            {
                return;
            }

            var isVisible = UserSession.IsTrusted;
            item.IsVisible = isVisible;

            if (!isVisible && ReferenceEquals(SelectedItem, item))
            {
                SelectedItem = MenuItems.FirstOrDefault() as HamburgerMenuItem;
            }
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
                    Tag = _addPartsConductorViewModel,
                    IsVisible = UserSession.IsTrusted
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
            var settings = new MetroDialogSettings { AffirmativeButtonText = Properties.Resources.Restart };

            await _updater.TryUpdateApplicationAsync(message => { StatusBarViewModel.Message = message; },
                () => StatusBarViewModel.Clear(),
                () => _dialogCoordinator.ShowMessageAsync(this,
                    Properties.Resources.RestartRequired,
                    Properties.Resources.RestartRequiredMessage,
                    settings: settings),
                () => UpdateManager.RestartApp());
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                UserSession.UserChanged -= HandleUserChanged;
            }

            _isDisposed = true;
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
            await UserSession.TryAutoLogOnAsync();
            await UpdateApplication();
        }

        public async Task EditProfileAsync()
        {
            await UserSession.EditProfileAsync();
        }

        public void Handle(OnDialogMessageBoxRequested message)
        {
            MessageBox.Show(message.Message, message.Caption, MessageBoxButton.OK, message.Icon);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
