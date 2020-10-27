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
using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using Autofac.Util;
using Stylet;

namespace BrickScan.WpfClient
{
    sealed class EventAggregationAutoSubscriptionModule : Autofac.Module
    {
        protected override void AttachToComponentRegistration(IComponentRegistryBuilder componentRegistry, IComponentRegistration registration)
        {
            registration.Activated += OnComponentActivated;
        }

        static void OnComponentActivated(object sender, ActivatedEventArgs<object> args)
        {
            var handler = args.Instance as IHandle;
            
            if (handler == null)
            {
                return;
            }

            var context = args.Context;
            var lifetimeScope = context.Resolve<ILifetimeScope>();
            var eventAggregator = lifetimeScope.Resolve<IEventAggregator>();

            eventAggregator.Subscribe(handler);

            var disposableAction = new DisposableAction(() =>
            {
                eventAggregator.Unsubscribe(handler);
            });

            lifetimeScope.Disposer.AddInstanceForDisposal(disposableAction);
        }
    }

    sealed class DisposableAction : Disposable
    {
        public DisposableAction(Action action)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        private Action Action { get; }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Action.Invoke();
            }
        }
    }

    internal class AutofacBootstrapper<TRootViewModel> : BootstrapperBase where TRootViewModel : class
    {
#pragma warning disable 8618
        private IContainer _container;
#pragma warning restore 8618

        private object? _rootViewModel;
        protected virtual object RootViewModel => _rootViewModel ??= GetInstance(typeof(TRootViewModel));

        private void ConfigureBaseIoC(ContainerBuilder builder)
        {
            var viewManagerConfig = new ViewManagerConfig
            {
                ViewFactory = GetInstance,
                ViewAssemblies = new List<Assembly> { GetType().Assembly }
            };

            builder.RegisterInstance<IViewManager>(new ViewManager(viewManagerConfig));

            builder.RegisterInstance<IWindowManagerConfig>(this).ExternallyOwned();
            builder.RegisterType<WindowManager>().As<IWindowManager>().SingleInstance();
            builder.RegisterType<EventAggregator>().As<IEventAggregator>().SingleInstance(); 
            builder.RegisterType<MessageBoxViewModel>().As<IMessageBoxViewModel>().ExternallyOwned();
            builder.RegisterModule<EventAggregationAutoSubscriptionModule>();
        }

        protected override void ConfigureBootstrapper()
        {
            var builder = new ContainerBuilder();
            ConfigureBaseIoC(builder);
            ConfigureIoC(builder);
            _container = builder.Build();
        }

        protected virtual void ConfigureIoC(ContainerBuilder builder) { }

        public override object GetInstance(Type type)
        {
            return _container.Resolve(type);
        }

        protected override void Launch()
        {
            base.DisplayRootView(RootViewModel);
        }

        public override void Dispose()
        {
            ScreenExtensions.TryDispose(_rootViewModel);
            _container.Dispose();
            base.Dispose();
        }
    }
}
