using Arction.Wpf.Charting;
using Arction.Wpf.Charting.SeriesXY;
using CotrollerDemo.ViewModels;
using DevExpress.Xpf.Editors;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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

            if (Application.Current.MainWindow is MainWindow mainView)
            {
                _main = mainView;
                _main.LayoutUpdated += MainView_LayoutUpdated;
            }
        }

        private ControllerViewModel _controller;
        private readonly MainWindow _main = new();

        private void MainView_LayoutUpdated(object sender, EventArgs e)
        {
            double newHeight = _main.ActualHeight - 80;
            FileGroup.Height = newHeight;
            if (FileGroup.Height >= 100)
            {
                FileList.Height = FileGroup.Height - 100;
            }
        }

        private void CanvasBase_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _controller = DataContext as ControllerViewModel;
            if (_controller != null && !CanvasBase.Children.Contains(_controller.Charts[0]))
            {
                CanvasBase.Children.Add(_controller.Charts[0]);
            }

            if (_controller != null)
            {
                _controller.Charts[0].Width = CanvasBase.ActualWidth;
                _controller.Charts[0].Height = CanvasBase.ActualHeight;
            }
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
                    if (sender is ListBoxEdit { SelectedItem: not null } listBoxEdit)
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
        private void ChartDockGroup_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string path = e.Data.GetData(DataFormats.StringFormat) as string;

                string filePath = System.IO.Path.Combine("D:\\Datas", path ?? string.Empty);

                if (File.Exists(filePath))
                {
                    // 读取文件的所有行并存储到数组中
                    string[] lines = File.ReadAllLines(filePath);
                    string[][] data = new string[lines.Length][];
                    float[] yData = new float[lines.Length];

                    for (int i = 0; i < lines.Length; i++)
                    {
                        data[i] = lines[i].Split(['-'], 2, StringSplitOptions.RemoveEmptyEntries);
                        yData[i] = Convert.ToSingle(Math.Round(Convert.ToDouble(data[i][1]), 6));
                    }

                    if (path != null)
                    {
                        string title = path.Split('.')[0];
                        _controller.Charts[0].BeginUpdate();

                        SampleDataSeries series = new(_controller.Charts[0].ViewXY, _controller.Charts[0].ViewXY.XAxes[0],
                            _controller.Charts[0].ViewXY.YAxes[0])
                        {
                            Title = new Arction.Wpf.Charting.Titles.SeriesTitle() { Text = title }, // 设置曲线标题
                            LineStyle =
                            {
                                Color = ChartTools.CalcGradient(_controller.GenerateUniqueColor(), Colors.White, 50),
                            },
                            SampleFormat = SampleFormat.SingleFloat
                        };

                        series.MouseOverOn += (_, _) => { _controller.Sample = series; };

                        _controller.CreateAnnotation(_controller.Charts[0]);

                        series.AddSamples(yData, false);

                        _controller.Charts[0].ViewXY.SampleDataSeries.Add(series);
                    }

                    _controller.Charts[0].ViewXY.LineSeriesCursors[0].Visible = true;

                    _controller.Charts[0].EndUpdate();

                    _controller.UpdateCursorResult(_controller.Charts[0]);
                }
            }
        }
    }
}