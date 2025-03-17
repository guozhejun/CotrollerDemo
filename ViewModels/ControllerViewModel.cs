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

        public ObservableCollection<string> FileNames
        {
            get { return _fileNames; }
            set { SetProperty(ref _fileNames, value); }
        }

        private ObservableCollection<DeviceInfoModel> _devices = [];
        public ObservableCollection<DeviceInfoModel> Devices
        {
            get { return _devices; }
            set { SetProperty(ref _devices, value); }
        }

        private object _chartContent = new();
        public object ChartContent
        {
            get { return _chartContent; }
            set { SetProperty(ref _chartContent, value); }
        }

        public LightningChart _chart { get; set; } = new();

        private List<SeriesPoint[]> _dataSeries = [];

        private int _pointCount = 0;

        private const int MaxPoints = 1024;

        private int _seriseCount = 8;

        private DispatcherTimer _timer;

        private static Random random = new();

        private static HashSet<Color> generatedColors = [];

        // 存放路径
        public string folderPath = @"D:\Coding\Datas";

        private bool _isRunning;
        public bool IsRunning
        {
            get { return _isRunning; }
            set
            {
                _chart.ViewXY.LineSeriesCursors[0].Visible = !value;
                _chart.ViewXY.Annotations[0].Visible = !value;
                SetProperty(ref _isRunning, value);
            }
        }

        public DelegateCommand DeviceSearchCommand { get; set; }

        public DelegateCommand SaveDataCommand { get; set; }

        public AsyncDelegateCommand StartTestCommand { get; set; }

        public DelegateCommand StopTestCommand { get; set; }

        public DelegateCommand OpenFolderCommand { get; set; }

        public DelegateCommand ClearFolderCommand { get; set; }

        public DelegateCommand<object> ConnectCommand { get; set; }

        public DelegateCommand<object> DisconnectCommand { get; set; }

        public DelegateCommand<object> SwitchLegendCommand { get; set; }

        public DelegateCommand<object> DeleteFileCommand { get; set; }

        public ControllerViewModel()
        {
            CreateChart();

            ChartContent = _chart;

            UpdateDeviceList();
            GlobalValues.TcpClient.StartTcpListen("192.168.1.37");

            StartTestCommand = new AsyncDelegateCommand(StartChart, CanStartChart).ObservesProperty(() => IsRunning);
            StopTestCommand = new DelegateCommand(StopTest, CanStopChart).ObservesProperty(() => IsRunning);
            SaveDataCommand = new DelegateCommand(SaveData);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            ClearFolderCommand = new DelegateCommand(ClearFolder);
            ConnectCommand = new DelegateCommand<object>(ConnectDevice);
            DisconnectCommand = new DelegateCommand<object>(DisconnectDevice);
            SwitchLegendCommand = new DelegateCommand<object>(SwitchLegend);
            DeleteFileCommand = new DelegateCommand<object>(DeleteFile);
            GetFolderFiles();
        }

        /// <summary>
        /// 创建图表
        /// </summary>
        private void CreateChart()
        {
            _chart.BeginUpdate();

            _chart.Title.Visible = false;

            ///只允许水平平移和鼠标滚轮缩放
            _chart.ViewXY.ZoomPanOptions.PanDirection = PanDirection.Horizontal;
            _chart.ViewXY.ZoomPanOptions.WheelZooming = WheelZooming.Horizontal;

            ViewXY view = _chart.ViewXY;

            view.XAxes[0].SetRange(0, 1024); // 设置X轴范围
            view.XAxes[0].SweepingGap = 0; // 设置X轴滚动间隔
            view.XAxes[0].ValueType = AxisValueType.Number; // 设置X轴数据类型
            view.XAxes[0].AutoFormatLabels = false; // 设置X轴标签自动格式化
            view.XAxes[0].LabelsNumberFormat = "N0"; // 设置X轴标签格式
            view.XAxes[0].MajorGrid.Pattern = LinePattern.Solid; // 设置X轴网格线样式
            view.XAxes[0].Title = null;
            view.XAxes[0].MajorGrid.Visible = false;
            view.XAxes[0].MinorGrid.Visible = false;

            view.LegendBoxes[0].Layout = LegendBoxLayout.Vertical;
            view.LegendBoxes[0].Fill.Color = Colors.Transparent;
            view.LegendBoxes[0].Shadow.Color = Colors.Transparent;

            view.AxisLayout.AxisGridStrips = XYAxisGridStrips.X;
            view.AxisLayout.YAxesLayout = YAxesLayout.Stacked; // 设置Y轴布局
            view.AxisLayout.SegmentsGap = 2; // 设置Y轴间隔
            view.AxisLayout.YAxisAutoPlacement = YAxisAutoPlacement.LeftThenRight; // 设置Y轴标题位置
            view.AxisLayout.YAxisTitleAutoPlacement = true; // 设置Y轴标题自动位置

            view.AxisLayout.AutoAdjustMargins = false; // 设置是否自动调整边距

            DisposeAllAndClear(view.PointLineSeries);
            DisposeAllAndClear(view.YAxes);

            Color color = Colors.Black;

            // 创建8条曲线，每条曲线颜色不同
            for (int i = 0; i < _seriseCount; i++)
            {
                Color lineBaseColor = GenerateUniqueColor();
                // 创建新的Y轴
                var yAxis = new AxisY(view);
                yAxis.Title.Text = $"Y{i + 1}"; // 设置Y轴标题
                yAxis.Title.Visible = true;
                yAxis.Title.Angle = 0;
                yAxis.Title.Color = lineBaseColor;
                yAxis.Units.Text = null;
                yAxis.Units.Visible = false;
                yAxis.AllowScaling = false;
                yAxis.MajorGrid.Visible = false;
                yAxis.MinorGrid.Visible = false;
                yAxis.MajorGrid.Pattern = LinePattern.Solid;
                yAxis.AutoDivSeparationPercent = 0;
                yAxis.Visible = true;
                yAxis.SetRange(0, 100); // 设置Y轴范围
                yAxis.MajorGrid.Color = Colors.LightGray;
                view.YAxes.Add(yAxis);

                if (i == _dataSeries.Count - 1)
                {
                    yAxis.MiniScale.ShowX = true;
                    yAxis.MiniScale.ShowY = true;
                    yAxis.MiniScale.Color = Color.FromArgb(255, 255, 204, 0);
                    yAxis.MiniScale.HorizontalAlign = AlignmentHorizontal.Right;
                    yAxis.MiniScale.VerticalAlign = AlignmentVertical.Bottom;
                    yAxis.MiniScale.Offset = new PointIntXY(-30, -30);
                    yAxis.MiniScale.LabelX.Color = Colors.White;
                    yAxis.MiniScale.LabelY.Color = Colors.White;
                    yAxis.MiniScale.PreferredSize = new SizeDoubleXY(50, 50);
                }

                var series = new PointLineSeries(view, view.XAxes[0], yAxis)
                {
                    Title = new Arction.Wpf.Charting.Titles.SeriesTitle() { Text = $"Curve {i + 1}" }, // 设置曲线标题
                    ScrollModePointsKeepLevel = 1,
                    AllowUserInteraction = true,
                    LineStyle = { Color = ChartTools.CalcGradient(lineBaseColor, Colors.White, 50) }
                };

                series.MouseDoubleClick += (s, e) =>
                {
                    var TemporarySeries = s as PointLineSeries;

                    var title = TemporarySeries.Title.Text.Split(':');

                    DialogResult result = (DialogResult)DXMessageBox.Show($"是否删除{title[0]}曲线?", "提示", MessageBoxButton.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        _chart.ViewXY.PointLineSeries.Remove(series);
                        _chart.ViewXY.YAxes.Remove(yAxis);
                        UpdateCursorResult();
                    }
                };

                view.PointLineSeries.Add(series);

                _dataSeries.Add(new SeriesPoint[MaxPoints]);
                for (int j = 0; j < MaxPoints; j++)
                {
                    _dataSeries[i][j] = new SeriesPoint(j, 0); // 初始数据为0
                }

            }
            //添加注释以显示游标值
            AnnotationXY cursorValueDisplay = new(_chart.ViewXY, _chart.ViewXY.XAxes[0], _chart.ViewXY.YAxes[0])
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
            _chart.ViewXY.Annotations.Add(cursorValueDisplay);

            //添加光标
            LineSeriesCursor cursor = new(_chart.ViewXY, _chart.ViewXY.XAxes[0]);
            cursor.ValueAtXAxis = 100;
            cursor.Visible = false;
            cursor.LineStyle.Color = Color.FromArgb(150, 255, 0, 0);
            cursor.SnapToPoints = true;
            cursor.TrackPoint.Color1 = Colors.White;
            _chart.ViewXY.LineSeriesCursors.Add(cursor);
            cursor.PositionChanged += cursor_PositionChanged;

            _chart.ViewXY.ZoomToFit();

            _chart.AfterRendering += _chart_AfterRendering;

            _chart.EndUpdate();
            _chart.SizeChanged += new SizeChangedEventHandler(_chart_SizeChanged);

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
            _chart.ViewXY.LegendBoxes[0].Visible = !_chart.ViewXY.LegendBoxes[0].Visible;
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

                if (IsTcpListenerClosed(GlobalValues.TcpClient.Tcp))
                {
                    GlobalValues.TcpClient.Tcp = new TcpListener(IPAddress.Any, 9089);
                    GlobalValues.TcpClient.Tcp.Start();
                }

                Devices = await GlobalValues.UdpClient.IsConnectDevice(linkIP.IpAddress, true);
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

                Devices = await GlobalValues.UdpClient.IsConnectDevice(linkIP.IpAddress, false);

                GlobalValues.TcpClient.client?.Close();
                GlobalValues.TcpClient.stream?.Close();
                GlobalValues.TcpClient.Tcp?.Stop();
            });

        }

        /// <summary>
        /// 更新设备列表
        /// </summary>
        private void UpdateDeviceList()
        {
            ObservableCollection<DeviceInfoModel> devices = [];
            Task.Run(async () =>
            {
                Devices = await GlobalValues.UdpClient.StartListen("192.168.1.37");
            });

        }

        /// <summary>
        /// 开始试验
        /// </summary>
        private async Task StartChart()
        {
            await GlobalValues.TcpClient.SendDataClient(1);

            //_timer = new()
            //{
            //    Interval = TimeSpan.FromMilliseconds(1), // 更新间隔
            //};
            //_timer.Tick += OnTimerTick;
            //_timer.Start();

            IsRunning = true; // 更新运行状态
        }

        /// <summary>
        /// 停止试验
        /// </summary>
        private void StopTest()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer = null;
            }

            IsRunning = false; // 更新运行状态
        }

        /// <summary>
        /// 更新光标位置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cursor_PositionChanged(object sender, PositionChangedEventArgs e)
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
            AxisY axisY = _chart.ViewXY.YAxes[series.AssignYAxisIndex];
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

        private void _chart_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCursorResult();
        }

        private void _chart_AfterRendering(object sender, AfterRenderingEventArgs e)
        {
            _chart.AfterRendering -= _chart_AfterRendering;
            UpdateCursorResult();
        }

        /// <summary>
        /// 更新光标结果
        /// </summary>
        public void UpdateCursorResult()
        {
            _chart.BeginUpdate();

            //获取光标
            LineSeriesCursor cursor = _chart.ViewXY.LineSeriesCursors[0];

            //获取注释
            AnnotationXY cursorValueDisplay = _chart.ViewXY.Annotations[0];

            float targetYCoord = (float)_chart.ViewXY.GetMarginsRect().Bottom;
            _chart.ViewXY.YAxes[0].CoordToValue(targetYCoord, out double y);

            cursorValueDisplay.TargetAxisValues.X = cursor.ValueAtXAxis;
            cursorValueDisplay.TargetAxisValues.Y = y;


            StringBuilder sb = new();
            int seriesNumber = 1;

            string value;

            foreach (PointLineSeries series in _chart.ViewXY.PointLineSeries)
            {

                //如果批注中的光标值没有显示在光标旁边，则在图表的右侧显示其中的系列标题和光标值
                series.Title.Visible = false;
                string title = series.Title.Text.Split(':')[0];
                bool resolvedOK = false;

                resolvedOK = SolveValueAccurate(series, cursor.ValueAtXAxis, out double seriesYValue);

                AxisY axisY = _chart.ViewXY.YAxes[series.AssignYAxisIndex];

                value = string.Format("{0}: {1,12:#####.###} {2}", title, seriesYValue.ToString("0.0"), axisY.Units.Text);

                sb.AppendLine(value);
                series.Title.Text = value;
                seriesNumber++;
            }

            sb.AppendLine("");
            sb.AppendLine("X: " + cursor.ValueAtXAxis.ToString());

            //设置文本
            cursorValueDisplay.Text = sb.ToString();

            cursorValueDisplay.Visible = !IsRunning;

            _chart.EndUpdate();
        }

        /// <summary>
        /// 定时器触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimerTick(object sender, EventArgs e)
        {
            PointLineSeries series = new();

            _chart.BeginUpdate();


            for (int i = 0; i < _chart.ViewXY.PointLineSeries.Count; i++)
            {
                double y = random.NextDouble() * 100; // 生成随机数据
                _dataSeries[i][_pointCount] = new SeriesPoint(_pointCount, y);

                series = _chart.ViewXY.PointLineSeries[i];
                series.Points = _dataSeries[i];

            }

            _pointCount++;

            if (_pointCount >= MaxPoints)
            {
                SaveData();
                _pointCount = 0;

                _dataSeries = [];

                for (int i = 0; i < _seriseCount; i++)
                {
                    _dataSeries.Add(new SeriesPoint[MaxPoints]);
                    for (int j = 0; j < MaxPoints; j++)
                    {
                        _dataSeries[i][j] = new SeriesPoint(j, 0); // 初始数据为0
                    }
                }
            }

            _chart.EndUpdate();
        }

        /// <summary>
        /// 保存折线数据
        /// </summary>
        private void SaveData()
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
                        sw.WriteLine($"{i}-{_dataSeries[j][i].Y}");
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
