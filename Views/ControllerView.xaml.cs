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

        private void ContentBase_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Controller = DataContext as ControllerViewModel;
            Controller._chart.Width = ContentBase.ActualWidth;
            Controller._chart.Height = ContentBase.ActualHeight;
        }

        private void ListBoxEdit_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is ListBoxEdit listBoxEdit && listBoxEdit.SelectedItem != null)
                    DragDrop.DoDragDrop(listBoxEdit, listBoxEdit.SelectedItem, DragDropEffects.Copy);
            }
        }

        public void LightingChartItem_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string data = e.Data.GetData(DataFormats.StringFormat) as string;

                string filePath = System.IO.Path.Combine("D:\\Coding\\Datas", data);

                if (File.Exists(filePath))
                {
                    // 读取文件的所有行并存储到数组中
                    string[] lines = File.ReadAllLines(filePath);
                    string[][] datas = new string[lines.Length][];

                    for (int i = 0; i < lines.Length; i++)
                    {
                        datas[i] = lines[i].Split(['-'], StringSplitOptions.RemoveEmptyEntries);
                    }

                    if (data != null)
                    {
                        Controller._chart.BeginUpdate();

                        // 创建新的Y轴
                        var yAxis = new AxisY(Controller._chart.ViewXY);
                        yAxis.Title.Visible = false;
                        yAxis.Units.Visible = false;
                        yAxis.AllowScaling = false;
                        yAxis.MajorGrid.Visible = false;
                        yAxis.MinorGrid.Visible = false;
                        yAxis.MajorGrid.Pattern = LinePattern.Solid;
                        yAxis.Units.Text = null;
                        yAxis.AutoDivSeparationPercent = 0;
                        yAxis.Visible = true;
                        yAxis.SetRange(0, 100); // 设置Y轴范围
                        Controller._chart.ViewXY.YAxes.Add(yAxis);


                        PointLineSeries series = new(Controller._chart.ViewXY, Controller._chart.ViewXY.XAxes[0], yAxis)
                        {
                            Title = new Arction.Wpf.Charting.Titles.SeriesTitle() { Text = data }, // 设置曲线标题

                            LineStyle = { Color = ChartTools.CalcGradient(Controller.GenerateUniqueColor(), Colors.White, 50) },
                            Points = new SeriesPoint[datas.Length]
                        };



                        series.MouseDoubleClick += (s, e) =>
                        {
                            var TemporarySeries = s as PointLineSeries;

                            var title = TemporarySeries.Title.Text.Split(':');

                            DialogResult result = (DialogResult)DXMessageBox.Show($"是否删除{title[0]}曲线?", "提示", MessageBoxButton.YesNo);

                            if (result == DialogResult.Yes)
                            {
                                Controller._chart.ViewXY.PointLineSeries.Remove(series);
                                Controller._chart.ViewXY.YAxes.Remove(yAxis);
                                Controller.UpdateCursorResult();
                            }
                        };

                        for (int pointIndex = 0; pointIndex < datas.Length; pointIndex++)
                        {
                            series.Points[pointIndex].X = Convert.ToDouble(datas[pointIndex][0]);
                            series.Points[pointIndex].Y = Convert.ToDouble(datas[pointIndex][1]);
                        }

                        Controller._chart.ViewXY.PointLineSeries.Add(series);

                        Controller._chart.EndUpdate();

                        Controller.UpdateCursorResult();
                    }
                }

            }
        }

    }
}