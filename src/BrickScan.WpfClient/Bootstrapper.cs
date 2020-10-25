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
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Windows.Threading;
using Autofac;
using AutofacSerilogIntegration;
using BrickScan.WpfClient.Extensions;
using BrickScan.WpfClient.Model;
using BrickScan.WpfClient.Properties;
using BrickScan.WpfClient.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Identity.Client;
using Serilog;
using VideoCapture = BrickScan.WpfClient.Model.VideoCapture;

namespace BrickScan.WpfClient
{
    internal class Bootstrapper : AutofacBootstrapper<MainViewModel>
    {
        private void SetupLanguage()
        {
            var cultureInfo = new CultureInfo(Settings.Default.SelectedCultureKey);
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            Log.ForContext<Bootstrapper>().Information("Set up culture = {CultureName}.",
                Settings.Default.SelectedCultureKey);
        }

        private static void LogCallback(LogLevel level, string message, bool containsPii)
        {
            Log.ForContext(typeof(IPublicClientApplication))
                .Write(level.ToSerilogLogEventLevel(), message);
        }

        protected override void ConfigureBootstrapper()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            Log.ForContext<Bootstrapper>().Information("Set up logger.");

            SetupLanguage();

            base.ConfigureBootstrapper();
        }

        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            Log.ForContext<Bootstrapper>().Fatal(e.Exception, e.Exception.Message);
        }

        protected override void ConfigureIoC(ContainerBuilder builder)
        {
            builder.Register(c => PublicClientApplicationBuilder.Create(IdentitySettings.ClientId)
                    .WithB2CAuthority(IdentitySettings.AuthoritySignUpSignIn)
                .WithRedirectUri(IdentitySettings.RedirectUri)
                .WithLogging(LogCallback, LogLevel.Info, false)
                .Build())
                .As<IPublicClientApplication>()
                .SingleInstance();

            builder.RegisterType<UserSession>()
                .As<IUserSession>()
                .SingleInstance()
                .AutoActivate();

            builder.RegisterType<VideoCapture>().As<IVideoCapture>().SingleInstance();
            builder.RegisterType<RoiDetector>().As<IRoiDetector>();
            builder.Register(c => DialogCoordinator.Instance).As<IDialogCoordinator>();
            builder.RegisterGeneric(typeof(NotifyTask<>)).AsSelf();

            builder.Register(c =>
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    return new HttpClient
                    {
                        BaseAddress = new Uri(ConfigurationManager.AppSettings["BrickScanApiBaseUrl"])
                    };
                })
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<BrickScanApiClient>().As<IBrickScanApiClient>();
            builder.Register(c => UserConfiguration.Instance).As<IUserConfiguration>();
            builder.RegisterType<PredictedClassViewModelFactory>().As<IPredictedClassViewModelFactory>().SingleInstance();
            builder.RegisterLogger();
        }
    }
}