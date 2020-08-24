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

using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using Stylet;
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global

namespace BrickScan.WpfClient.ViewModels
{
    public class MainViewModel : Screen
    {
        private readonly PredictViewModel _predictViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        public StatusBarViewModel StatusBarViewModel { get; }

        public HamburgerMenuItemCollection MenuItems => BuildMenuItems();

        public HamburgerMenuItemCollection MenuOptionItems => BuildMenuOptionsItems();

        public HamburgerMenuItem SelectedMenuItem { get; set; }

        public MainViewModel(PredictViewModel predictViewModel, 
            SettingsViewModel settingsViewModel, 
            StatusBarViewModel statusBarViewModel)
        {
            _predictViewModel = predictViewModel;
            _settingsViewModel = settingsViewModel;
            StatusBarViewModel = statusBarViewModel;
        }

        private HamburgerMenuItemCollection BuildMenuItems()
        {
            var collection = new HamburgerMenuItemCollection
            {
                new HamburgerMenuIconItem
                {
                    Icon = new PackIconMaterial{ Kind = PackIconMaterialKind.Home },
                    Label = Properties.Resources.Home.ToUpperInvariant(),
                    Tag = _predictViewModel
                },
                new HamburgerMenuIconItem
                {
                    Icon = new PackIconMaterial{ Kind = PackIconMaterialKind.ArmFlex },
                    Label = Properties.Resources.Contribute.ToUpperInvariant(),
                    Tag = null
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

        public void CloseApplication()
        {
            RequestClose();
        }

        public void LaunchBrickScanOnGithub()
        {
            System.Diagnostics.Process.Start("https://github.com/gebirgslok/BrickScan");
        }
    }
}