using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Annotations;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.SeriesXY;
using CotrollerDemo.Models;
using CotrollerDemo.ViewModels;
using DevExpress.Utils.CommonDialogs.Internal;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Core.ConditionalFormattingManager;
using DevExpress.Xpf.Editors;
using DXApplication.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CotrollerDemo.Views
{
    /// <summary>
    /// ControllerView.xaml 的交互逻辑
    /// </summary>
    public partial class ControllerView : UserControl
    {
        public ControllerView()
        {
            InitializeComponent();

            var mainView = Application.Current.MainWindow as MainWindow;
            if (mainView != null)
            {
                main = mainView;
                mainView.LayoutUpdated += MainView_LayoutUpdated;
            }
        }

        ControllerViewModel Controller;
        MainWindow main = new();

        private void MainView_LayoutUpdated(object sender, EventArgs e)
        {
            double newHeight = main.ActualHeight - 80;
            fileGroup.Height = newHeight;
            if (fileGroup.Height >= 100)
            {
                fileList.Height = fileGroup.Height - 100;
            }

        }

        private void CanvasBase_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Controller = DataContext as ControllerViewModel;
            if (!CanvasBase.Children.Contains(Controller.Chart))
            {
                CanvasBase.Children.Add(Controller.Chart);
            }
            Controller.Chart.Width = CanvasBase.ActualWidth;
            Controller.Chart.Height = CanvasBase.ActualHeight;

        }

        /// <summary>
        /// 拖拽文件触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxEdit_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (sender is ListBoxEdit listBoxEdit && listBoxEdit.SelectedItem != null)
                        DragDrop.DoDragDrop(listBoxEdit, listBoxEdit.SelectedItem, DragDropEffects.Copy);
                }
            }
            catch (ArgumentNullException)
            {
            }
        }

        /// <summary>
        /// 拖拽文件到曲线图中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void LightingChartItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string data = e.Data.GetData(DataFormats.StringFormat) as string;

                string filePath = System.IO.Path.Combine("D:\\Datas", data);

                if (File.Exists(filePath))
                {
                    // 读取文件的所有行并存储到数组中
                    string[] lines = File.ReadAllLines(filePath);
                    string[][] datas = new string[lines.Length][];
                    float[] YDatas = new float[lines.Length];

                    for (int i = 0; i < lines.Length; i++)
                    {
                        datas[i] = lines[i].Split(['-'], StringSplitOptions.RemoveEmptyEntries);
                        YDatas[i] = Convert.ToSingle(datas[i][1]);
                    }

                    if (data != null)
                    {
                        Controller.Chart.BeginUpdate();

                        SampleDataBlockSeries series = new(Controller.Chart.ViewXY, Controller.Chart.ViewXY.XAxes[0], Controller.Chart.ViewXY.YAxes[0])
                        {
                            Title = new Arction.Wpf.Charting.Titles.SeriesTitle() { Text = data }, // 设置曲线标题
                            Color = ChartTools.CalcGradient(Controller.GenerateUniqueColor(), Colors.White, 50),
                        };

                        series.MouseDoubleClick += (s, e) =>
                        {
                            var TemporarySeries = s as SampleDataBlockSeries;

                            var title = TemporarySeries.Title.Text.Split(':');

                            DialogResult result = (DialogResult)DXMessageBox.Show($"是否删除{title[0]}曲线?", "提示", MessageBoxButton.YesNo);

                            if (result == DialogResult.Yes)
                            {
                                Controller.Chart.ViewXY.SampleDataBlockSeries.Remove(series);
                                Controller.UpdateCursorResult();
                            }
                        };

                        series.AddSamples(YDatas, false);

                        Controller.Chart.ViewXY.SampleDataBlockSeries.Add(series);

                        Controller.Chart.ViewXY.LineSeriesCursors[0].Visible = true;

                        Controller.Chart.EndUpdate();

                        Controller.UpdateCursorResult();
                    }
                }

            }
        }

        private void AddCommentBtn_Click(object sender, RoutedEventArgs e)
        {
            var textBox = new ResizableTextBox
            {
                Text = "双击编辑文字",
                Foreground = Brushes.Black
            };

            Canvas.SetLeft(textBox, 50);
            Canvas.SetTop(textBox, 50);

            CanvasBase.Children.Add(textBox);
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}