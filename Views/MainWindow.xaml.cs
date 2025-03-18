using CotrollerDemo.Models;
using System;
using System.Threading.Tasks;
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
            this.Closed += MainWindow_Closed;
        }



        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

    }
}
