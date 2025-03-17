using Prism.Ioc;
using CotrollerDemo.Views;
using System.Windows;
using CotrollerDemo.ViewModels;
using Prism.DryIoc;
using Prism.Navigation.Regions;
using Prism.Modularity;
using ControllViewModule;

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

        //protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        //{
        //    moduleCatalog.AddModule<ControllViewModuleModule>();
        //}

        protected override void Initialize()
        {
            base.Initialize();

            var shell = MainWindow;
            shell.Show();

            // 导航到ViewA
            var regionManager = Container.Resolve<IRegionManager>();
            //regionManager.RequestNavigate("ContentRegion", "ControlView");
            regionManager.RequestNavigate("ContentRegion", "ControllerView");

        }
    }
}
