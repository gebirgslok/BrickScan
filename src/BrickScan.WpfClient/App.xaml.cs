using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Navigation;
using BrickScan.WpfClient.Properties;
using ControlzEx.Theming;
using Serilog;

namespace BrickScan.WpfClient
{
    public partial class App
    {
        public App()
        {
            Settings.Default.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args)
            {
                if (args.PropertyName == nameof(Settings.Default.ThemeBaseColor) ||
                    args.PropertyName == nameof(Settings.Default.ThemeColorScheme))
                {
                    ChangeTheme();
                }
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ChangeTheme();
        }

        private void ChangeTheme()
        {
            var themeName = $"{Settings.Default.ThemeBaseColor}.{Settings.Default.ThemeColorScheme}";
            Log.ForContext<App>().Information("Changing theme to {ThemeName}", themeName);
            ThemeManager.Current.ChangeTheme(this, themeName);
        }
    }
}
