using CotrollerDemo.ViewModels;
using CotrollerDemo.Views;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Navigation.Regions;
using System.Windows;

namespace CotrollerDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<ControllerView, ControllerViewModel>();
        }

        protected override void Initialize()
        {
            base.Initialize();

            var shell = MainWindow;
            shell.Show();

            // 导航到ViewA
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("ContentRegion", "ControllerView");
        }
    }
}