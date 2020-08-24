using Autofac;
using BrickScan.WpfClient.ViewModels;

namespace BrickScan.WpfClient
{
    internal class Bootstrapper : AutofacBootstrapper<MainViewModel>
    {
        protected override void ConfigureIoC(ContainerBuilder builder)
        {
            builder.RegisterType<PredictViewModel>().AsSelf();
            builder.RegisterType<MainViewModel>().AsSelf();
        }
    }
}