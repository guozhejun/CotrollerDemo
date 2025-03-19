using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Annotations;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.SeriesXY;
using Arction.Wpf.Charting.Views;
using Arction.Wpf.Charting.Views.ViewXY;
using CotrollerDemo.Models;
using DevExpress.Mvvm.Native;
using DevExpress.Utils;
using DevExpress.Xpf.Core;
using DryIoc.ImTools;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace CotrollerDemo.ViewModels
{
    public class ControllerViewModel : BindableBase
    {
        private ObservableCollection<string> _fileNames = [];

        /// <summary>
        /// 文件名集合
        /// </summary>
        public ObservableCollection<string> FileNames
        {
            get { return _fileNames; }
            set { SetProperty(ref _fileNames, value); }
        }

        private ObservableCollection<DeviceInfoModel> _devices = [];

        /// <summary>
        /// 设备列表
        /// </summary>
        public ObservableCollection<DeviceInfoModel> Devices
        {
            get { return _devices; }
            set { SetProperty(ref _devices, value); }
        }

        private object _chartContent = new();

        /// <summary>
        /// 图表内容
        /// </summary>
        public object ChartContent
        {
            get { return _chartContent; }
            set { SetProperty(ref _chartContent, value); }
        }

        public LightningChart Chart { get; set; } = new();

        private List<SeriesPoint[]> seriesPoints = [];

        public List<SeriesPoint[]> SeriesPoints
        {
            get { return seriesPoints; }
            set { SetProperty(ref seriesPoints, value); }
        }


        /// <summary>
        /// 生成点数    
        /// </summary>
        private int _pointCount = 0;

        /// <summary>
        /// 最大生成点数
        /// </summary>
        private const int MaxPoints = 1024;

        /// <summary>
        /// 曲线数量
        /// </summary>
        private int _seriseCount = 8;

        /// <summary>
        /// 存放已生成的颜色
        /// </summary>
        private static HashSet<Color> generatedColors = [];

        // 存放路径
        public string folderPath = @"D:\Datas";

        private bool _isRunning;
        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                GlobalValues.IsRunning = value;
                IsDrop = !value;
                SetProperty(ref _isRunning, value);
            }
        }

        private bool _isDrop;

        /// <summary>
        /// 是否可拖拽
        /// </summary>
        public bool IsDrop
        {
            get { return _isDrop; }
            set { SetProperty(ref _isDrop, value); }
        }

        /// <summary>
        /// 正弦波数据
        /// </summary>
        public List<List<float>> SineWaves { get; set; } = [];

        //public DelegateCommand<object> SaveDataCommand { get; set; }

        /// <summary>
        /// 开始试验
        /// </summary>
        public AsyncDelegateCommand StartTestCommand { get; set; }

        /// <summary>
        /// 停止试验
        /// </summary>
        public AsyncDelegateCommand StopTestCommand { get; set; }

        /// <summary>
        /// 查询设备
        /// </summary>
        public DelegateCommand DeviceQueryCommand { get; set; }

        /// <summary>
        /// 打开文件夹
        /// </summary>
        public DelegateCommand OpenFolderCommand { get; set; }

        /// <summary>
        /// 清空文件夹
        /// </summary>
        public DelegateCommand ClearFolderCommand { get; set; }

        /// <summary>
        /// 连接设备
        /// </summary>
        public DelegateCommand<object> ConnectCommand { get; set; }

        /// <summary>
        /// 断开连接
        /// </summary>
        public DelegateCommand<object> DisconnectCommand { get; set; }

        /// <summary>
        /// 切换图例
        /// </summary>
        public DelegateCommand<object> SwitchLegendCommand { get; set; }

        /// <summary>
        /// 删除文件
        /// </summary>
        public DelegateCommand<object> DeleteFileCommand { get; set; }

        /// <summary>
        /// 重置图表缩放
        /// </summary>
        public DelegateCommand ZoomToFitCommand { get; set; }

        public ControllerViewModel()
        {
            UpdateDeviceList();
            GetFolderFiles();
            CreateChart();
            UpdateSeriesData();

            ChartContent = Chart;
            IsRunning = false;
            GlobalValues.TcpClient.StartTcpListen();

            StartTestCommand = new AsyncDelegateCommand(StartChart, CanStartChart).ObservesProperty(() => IsRunning);
            StopTestCommand = new AsyncDelegateCommand(StopTest, CanStopChart).ObservesProperty(() => IsRunning);
            //SaveDataCommand = new DelegateCommand<object>(SaveData);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            DeviceQueryCommand = new DelegateCommand(UpdateDeviceList);
            ClearFolderCommand = new DelegateCommand(ClearFolder);
            ZoomToFitCommand = new DelegateCommand(ZoomToFitChart);
            ConnectCommand = new DelegateCommand<object>(ConnectDevice);
            DisconnectCommand = new DelegateCommand<object>(DisconnectDevice);
            SwitchLegendCommand = new DelegateCommand<object>(SwitchLegend);
            DeleteFileCommand = new DelegateCommand<object>(DeleteFile);
        }

        /// <summary>
        /// 重置图表缩放
        /// </summary>
        private void ZoomToFitChart()
        {
            Chart.ViewXY.ZoomToFit();
        }

        /// <summary>
        /// 创建图表
        /// </summary>
        private void CreateChart()
        {
            Chart.BeginUpdate();

            Chart.Title.Visible = false;

            ///只允许水平平移和鼠标滚轮缩放
            Chart.ViewXY.ZoomPanOptions.PanDirection = PanDirection.Horizontal;
            Chart.ViewXY.ZoomPanOptions.WheelZooming = WheelZooming.Horizontal;

            Color lineBaseColor = GenerateUniqueColor();

            ViewXY view = Chart.ViewXY;

            DisposeAllAndClear(view.PointLineSeries);
            DisposeAllAndClear(view.YAxes);

            // 设置X轴
            view.XAxes[0].ScrollMode = XAxisScrollMode.Scrolling; // 设置X轴范围
            view.XAxes[0].SetRange(0, 1024); // 设置X轴范围
            view.XAxes[0].ValueType = AxisValueType.Number; // 设置X轴数据类型
            view.XAxes[0].AutoFormatLabels = false; // 设置X轴标签自动格式化
            view.XAxes[0].LabelsNumberFormat = "N0"; // 设置X轴标签格式
            view.XAxes[0].MajorGrid.Pattern = LinePattern.Solid; // 设置X轴网格线样式
            view.XAxes[0].Title = null;
            view.XAxes[0].MajorGrid.Visible = false;
            view.XAxes[0].MinorGrid.Visible = false;

            // 设置Y轴
            var yAxis = new AxisY(view);
            yAxis.Title.Text = null; // 设置Y轴标题
            yAxis.Title.Visible = false;
            yAxis.Title.Angle = 0;
            yAxis.Title.Color = lineBaseColor;
            yAxis.Units.Text = null;
            yAxis.Units.Visible = false;
            yAxis.MajorGrid.Visible = false;
            yAxis.MinorGrid.Visible = false;
            yAxis.MajorGrid.Pattern = LinePattern.Solid;
            yAxis.AutoDivSeparationPercent = 0;
            yAxis.Visible = true;
            yAxis.SetRange(-5, 10); // 设置Y轴范围
            yAxis.MajorGrid.Color = Colors.LightGray;
            view.YAxes.Add(yAxis);

            // 设置图例
            view.LegendBoxes[0].Layout = LegendBoxLayout.Vertical;
            view.LegendBoxes[0].Fill.Color = Colors.Transparent;
            view.LegendBoxes[0].Shadow.Color = Colors.Transparent;

            // 设置Y轴
            view.AxisLayout.AxisGridStrips = XYAxisGridStrips.X;
            view.AxisLayout.YAxesLayout = YAxesLayout.Stacked; // 设置Y轴布局
            view.AxisLayout.SegmentsGap = 2; // 设置Y轴间隔
            view.AxisLayout.YAxisAutoPlacement = YAxisAutoPlacement.LeftThenRight; // 设置Y轴标题位置
            view.AxisLayout.YAxisTitleAutoPlacement = true; // 设置Y轴标题自动位置
            view.AxisLayout.AutoAdjustMargins = false; // 设置是否自动调整边距


            Color color = Colors.Black;

            // 创建8条曲线，每条曲线颜色不同
            for (int i = 0; i < _seriseCount; i++)
            {
                // 创建新的曲线
                var series = new PointLineSeries(view, view.XAxes[0], view.YAxes[0])
                {
                    Title = new Arction.Wpf.Charting.Titles.SeriesTitle() { Text = $"Curve {i + 1}" }, // 设置曲线标题
                    ScrollModePointsKeepLevel = 1,
                    PointsType = PointsType.Points,
                    AllowUserInteraction = true,
                    LineStyle = { Color = ChartTools.CalcGradient(GenerateUniqueColor(), Colors.White, 50) },
                };

                // 双击删除曲线
                series.MouseDoubleClick += (s, e) =>
                {
                    var TemporarySeries = s as PointLineSeries;

                    var title = TemporarySeries.Title.Text.Split(':');

                    DialogResult result = (DialogResult)DXMessageBox.Show($"是否删除{title[0]}曲线?", "提示", MessageBoxButton.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        Chart.ViewXY.PointLineSeries.Remove(series);
                        Chart.ViewXY.YAxes.Remove(yAxis);
                        UpdateCursorResult();
                    }
                };

                view.PointLineSeries.Add(series);

                seriesPoints.Add(new SeriesPoint[MaxPoints]);
                for (int j = 0; j < MaxPoints; j++)
                {
                    seriesPoints[i][j] = new SeriesPoint(); // 初始数据为0
                }

            }
            //添加注释以显示游标值
            AnnotationXY cursorValueDisplay = new(Chart.ViewXY, Chart.ViewXY.XAxes[0], Chart.ViewXY.YAxes[0])
            {
                Style = AnnotationStyle.RoundedCallout,
                LocationCoordinateSystem = CoordinateSystem.RelativeCoordinatesToTarget
            };
            cursorValueDisplay.LocationRelativeOffset.X = 130;
            cursorValueDisplay.LocationRelativeOffset.Y = -200;
            cursorValueDisplay.Sizing = AnnotationXYSizing.Automatic;
            cursorValueDisplay.TextStyle.Color = Colors.Black;
            cursorValueDisplay.Text = "";
            cursorValueDisplay.AllowTargetMove = false;
            cursorValueDisplay.Fill.Color = Colors.White;
            cursorValueDisplay.Fill.GradientColor = Color.FromArgb(120, color.R, color.G, color.B);
            cursorValueDisplay.BorderVisible = false;
            cursorValueDisplay.Visible = false;
            Chart.ViewXY.Annotations.Add(cursorValueDisplay);

            //添加光标
            LineSeriesCursor cursor = new(Chart.ViewXY, Chart.ViewXY.XAxes[0])
            {
                ValueAtXAxis = 100,
                Visible = true
            };
            cursor.LineStyle.Color = Color.FromArgb(150, 255, 0, 0);
            cursor.SnapToPoints = true;
            cursor.Behind = true;
            cursor.TrackPoint.Color1 = Colors.White;
            Chart.ViewXY.LineSeriesCursors.Add(cursor);
            cursor.PositionChanged += Cursor_PositionChanged;

            // 重置图表缩放
            Chart.ViewXY.ZoomToFit();

            Chart.AfterRendering += Chart_AfterRendering;

            Chart.EndUpdate();
            Chart.SizeChanged += new SizeChangedEventHandler(Chart_SizeChanged);

        }

        /// <summary>
        /// 切换图例状态
        /// </summary>
        /// <param name="obj"></param>
        private void SwitchLegend(object obj)
        {
            var btn = obj as SimpleButton;

            if (btn.Content.ToString() == "隐藏图例")
            {
                btn.Content = "显示图例";
            }
            else
            {
                btn.Content = "隐藏图例";
            }
            Chart.ViewXY.LegendBoxes[0].Visible = !Chart.ViewXY.LegendBoxes[0].Visible;
        }

        /// <summary>
        /// 是否可以停止绘制图表
        /// </summary>
        /// <returns></returns>
        private bool CanStopChart()
        {
            return IsRunning;
        }

        /// <summary>
        /// 是否可以开始绘制图表
        /// </summary>
        /// <returns></returns>
        private bool CanStartChart()
        {
            return !IsRunning;
        }

        /// <summary>
        /// 连接设备
        /// </summary>
        /// <param name="obj"></param>
        private void ConnectDevice(object obj)
        {
            Task.Run(async () =>
            {
                var selectItem = obj as DeviceInfoModel;

                var linkIP = Devices.First(d => d.IpAddress == selectItem.IpAddress);

                GlobalValues.UdpClient.IsConnectDevice(true);

                Devices = await GlobalValues.UdpClient.StartListen();
            });

        }

        /// <summary>
        /// 断开设备
        /// </summary>
        /// <param name="obj"></param>
        private void DisconnectDevice(object obj)
        {

            Task.Run(async () =>
            {
                var selectItem = obj as DeviceInfoModel;

                var linkIP = Devices.First(d => d.IpAddress == selectItem.IpAddress);

                GlobalValues.UdpClient.IsConnectDevice(false);

                Devices = await GlobalValues.UdpClient.StartListen();
            });

        }

        /// <summary>
        /// 更新设备列表
        /// </summary>
        private void UpdateDeviceList()
        {
            Task.Run(async () =>
            {
                Devices = await GlobalValues.UdpClient.StartListen();
            });

        }

        /// <summary>
        /// 开始试验
        /// </summary>
        private async Task StartChart()
        {
            if (GlobalValues.TcpClient.client != null)
            {
                await GlobalValues.TcpClient.SendDataClient(1);
            }

            int count = Chart.ViewXY.PointLineSeries.Count;

            if (count > 8)
            {
                for (int i = count; i > 8; i--)
                {
                    Chart.ViewXY.PointLineSeries.Remove(Chart.ViewXY.PointLineSeries[i - 1]);
                    Chart.ViewXY.YAxes.Remove(Chart.ViewXY.YAxes[i - 1]);
                }
            }

            IsRunning = true; // 更新运行状态
        }

        /// <summary>
        /// 停止试验
        /// </summary>
        private async Task StopTest()
        {
            if (GlobalValues.TcpClient.client != null)
            {
                await GlobalValues.TcpClient.SendDataClient(0);
            }

            IsRunning = false; // 更新运行状态
        }

        /// <summary>
        /// 更新曲线数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateSeriesData()
        {
            SineWaves = GlobalValues.TcpClient.SineWaveList;
            Task.Run(async () =>
            {
                while (true)
                {
                    if (IsRunning)
                    {
                        PointLineSeries series;
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Chart.BeginUpdate();
                            for (int i = 0; i < SineWaves.Count; i++)
                            {
                                if (_pointCount <= 1023 && SineWaves[i].Count >= 1024)
                                {
                                    series = Chart.ViewXY.PointLineSeries[i];
                                    series.AddPoints([new SeriesPoint(_pointCount, Convert.ToDouble(SineWaves[i][_pointCount]))], false);
                                }
                            }

                            _pointCount++;

                            if (_pointCount >= MaxPoints)
                            {
                                SaveData(SineWaves);

                                SineWaves = GlobalValues.TcpClient.SineWaveList;

                                _pointCount = 0;

                                Chart.ViewXY.ZoomToFit();

                                for (int i = 0; i < _seriseCount; i++)
                                {
                                    //seriesPoints.Add(new SeriesPoint[MaxPoints]);
                                    //for (int j = 0; j < MaxPoints; j++)
                                    //{
                                    //    seriesPoints[i][j] = new SeriesPoint(); // 初始数据为0
                                    //}
                                    Chart.ViewXY.PointLineSeries[i].Points = [];
                                }
                            }
                            Chart.EndUpdate();
                        });
                    }
                    await Task.Delay(10);
                }
            });

        }

        /// <summary>
        /// 更新光标位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cursor_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            //取消正在进行的呈现，因为下面的代码更新了图表。
            e.CancelRendering = true;

            UpdateCursorResult();
        }

        /// <summary>
        /// 根据X值解决Y值
        /// </summary>
        /// <param name="series"></param>
        /// <param name="xValue"></param>
        /// <param name="yValue"></param>
        /// <returns></returns>
        private bool SolveValueAccurate(PointLineSeries series, double xValue, out double yValue)
        {
            AxisY axisY = Chart.ViewXY.YAxes[series.AssignYAxisIndex];
            yValue = 0;

            LineSeriesValueSolveResult result = series.SolveYValueAtXValue(xValue);
            if (result.SolveStatus == LineSeriesSolveStatus.OK)
            {
                //PointLineSeries may have two or more points at same X value. If so, center it between min and max 
                yValue = (result.YMax + result.YMin) / 2.0;
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Chart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCursorResult();
        }

        private void Chart_AfterRendering(object sender, AfterRenderingEventArgs e)
        {
            Chart.AfterRendering -= Chart_AfterRendering;
            UpdateCursorResult();
        }

        /// <summary>
        /// 更新光标结果
        /// </summary>
        public void UpdateCursorResult()
        {
            Chart.BeginUpdate();

            //获取光标
            LineSeriesCursor cursor = Chart.ViewXY.LineSeriesCursors[0];

            //获取注释
            AnnotationXY cursorValueDisplay = Chart.ViewXY.Annotations[0];

            float targetYCoord = (float)Chart.ViewXY.GetMarginsRect().Bottom;
            Chart.ViewXY.YAxes[0].CoordToValue(targetYCoord, out double y);

            cursorValueDisplay.TargetAxisValues.X = cursor.ValueAtXAxis;
            cursorValueDisplay.TargetAxisValues.Y = y;


            StringBuilder sb = new();
            int seriesNumber = 1;

            string value;

            foreach (PointLineSeries series in Chart.ViewXY.PointLineSeries)
            {

                //如果批注中的光标值没有显示在光标旁边，则在图表的右侧显示其中的系列标题和光标值
                series.Title.Visible = false;
                string title = series.Title.Text.Split(':')[0];
                bool resolvedOK = false;

                resolvedOK = SolveValueAccurate(series, cursor.ValueAtXAxis, out double seriesYValue);

                AxisY axisY = Chart.ViewXY.YAxes[series.AssignYAxisIndex];

                value = string.Format("{0}: {1,12:#####.###} {2}", title, seriesYValue.ToString("0.0"), axisY.Units.Text);

                sb.AppendLine(value);
                series.Title.Text = value;
                seriesNumber++;
            }

            sb.AppendLine("");
            sb.AppendLine("X: " + cursor.ValueAtXAxis.ToString());

            //设置文本
            cursorValueDisplay.Text = sb.ToString();
            cursorValueDisplay.Visible = true;
            Chart.EndUpdate();
        }

        /// <summary>
        /// 保存折线数据
        /// </summary>
        private void SaveData(List<List<float>> Datas)
        {
            try
            {
                for (int j = 0; j < 8; j++) // 遍历每条曲线
                {
                    string fullPath = Path.Combine(folderPath, DateTime.Now.ToString("yyyyMMddHHmmss_") + $"Curve_{j + 1}.txt");
                    using StreamWriter sw = new(fullPath);
                    for (int i = 0; i < MaxPoints; i++)
                    {
                        // 写入 "X-Y" 格式的数据
                        sw.WriteLine($"{i}-{Convert.ToDouble(Datas[j][i])}");
                    }
                }
                // 检查文件夹是否存在，如果不存在则创建
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                GetFolderFiles();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("发生错误: " + ex.Message);
            }
        }

        /// <summary>
        /// 获取文件夹下的所有文件
        /// </summary>
        private void GetFolderFiles()
        {
            try
            {

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                FileNames.Clear();

                string[] files = Directory.GetFiles(folderPath);

                files.ForEach(file =>
                {
                    FileNames.Add(Path.GetFileName(file));
                });

            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// 释放所有并清除数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void DisposeAllAndClear<T>(List<T> list) where T : IDisposable
        {
            if (list == null)
            {
                return;
            }

            while (list.Count > 0)
            {
                int lastInd = list.Count - 1;
                T item = list[lastInd]; // take item ref from list. 
                list.RemoveAt(lastInd); // remove item first
                if (item != null)
                {
                    (item as IDisposable).Dispose();     // then dispose it. 
                }
            }
        }

        /// <summary>
        /// 随机生成颜色
        /// </summary>
        /// <returns></returns>
        public Color GenerateUniqueColor()
        {

            Color color;
            Random random = new();
            do
            {
                byte a = (byte)random.Next(256);
                byte b = (byte)random.Next(256);
                byte c = (byte)random.Next(256);
                color = Color.FromRgb(a, b, c);
            } while (generatedColors.Contains(color));

            generatedColors.Add(color);
            return color;
        }

        /// <summary>
        /// 判断 TcpListener 是否已关闭
        /// </summary>
        /// <param name="listener">TcpListener 实例</param>
        /// <returns>如果已关闭返回 true，否则返回 false</returns>
        public static bool IsTcpListenerClosed(TcpListener listener)
        {
            try
            {
                // 检查底层的 Socket 是否已关闭
                if (listener.Server == null)
                {
                    return true; // Server 为 null 表示已关闭
                }

                // 尝试访问 Socket 的属性来判断状态
                bool isClosed = !listener.Server.Connected;
                return isClosed;
            }
            catch (ObjectDisposedException)
            {
                // 如果 TcpListener 已经被释放，则说明已关闭
                return true;
            }
        }

        /// <summary>
        /// 打开文件夹
        /// </summary>
        private void OpenFolder()
        {
            try
            {
                Process.Start("explorer.exe", folderPath);
            }
            catch (Exception ex)
            {
                // 处理异常
                DXMessageBox.Show("无法打开文件夹: " + ex.Message);
            }
        }

        /// <summary>
        /// 清空文件夹
        /// </summary>
        private void ClearFolder()
        {
            try
            {
                if ((DialogResult)DXMessageBox.Show("是否清空文件夹?", "提示", MessageBoxButton.YesNo) == DialogResult.Yes)
                {
                    // 清空文件夹中的所有文件
                    if (!Directory.Exists(folderPath))
                    {
                        throw new DirectoryNotFoundException($"文件夹不存在: {folderPath}");
                    }

                    // 获取文件夹中的所有文件
                    string[] files = Directory.GetFiles(folderPath);

                    // 删除每个文件
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }

                    // 获取文件夹中的所有子文件夹
                    string[] subFolders = Directory.GetDirectories(folderPath);

                    // 递归删除子文件夹及其内容
                    foreach (string subFolder in subFolders)
                    {
                        Directory.Delete(subFolder, true); // true 表示递归删除
                    }

                    GetFolderFiles();
                    DXMessageBox.Show("文件夹已清空！");
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                DXMessageBox.Show("无法清空文件夹: " + ex.Message);
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="obj"></param>
        private void DeleteFile(object obj)
        {
            try
            {
                if ((DialogResult)DXMessageBox.Show("是否删除此文件?", "提示", MessageBoxButton.YesNo) == DialogResult.Yes)
                {
                    string fileName = obj as string;
                    string file = Path.Combine(folderPath, fileName);
                    File.Delete(file);

                    GetFolderFiles();
                    DXMessageBox.Show("文件已删除！");
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                DXMessageBox.Show("无法删除文件: " + ex.Message);
            }
        }
    }

}
