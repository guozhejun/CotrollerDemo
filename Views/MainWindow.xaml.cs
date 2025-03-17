using Prism.Navigation.Regions;
using System.Windows;

namespace CotrollerDemo.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 导航到ViewA
            var regionManager = RegionManager.GetRegionManager(this);
            regionManager.RequestNavigate("ContentRegion", "ControlView");
        }
    }
}
