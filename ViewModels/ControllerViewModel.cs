using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Annotations;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.Series3D;
using Arction.Wpf.Charting.SeriesXY;
using Arction.Wpf.Charting.Titles;
using Arction.Wpf.Charting.Views;
using Arction.Wpf.Charting.Views.ViewXY;
using CotrollerDemo.Models;
using CotrollerDemo.Views;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Pdf.Native;
using DevExpress.Utils;
using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Docking;
using DryIoc.ImTools;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CotrollerDemo.ViewModels
{
    public class ControllerViewModel : BindableBase
    {
        #region Property

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

        //public LightningChart Chart = new();

        public List<LightningChart> Charts { get; set; } = [];

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

        private int _chartCount = 1;

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

        /// <summary>
        /// 曲线点数数量
        /// </summary>
        public int[] PointNums = new int[8];

        private readonly object _renderLock = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentQueue<float>[] _dataBuffers;
        #endregion

        #region Command

        /// <summary>
        /// 存储数据
        /// </summary>
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

        /// <summary>
        /// 右键菜单
        /// </summary>
        public DelegateCommand<object> ShowMenuCommand { get; set; }

        /// <summary>
        /// 右键删除曲线
        /// </summary>
        public DelegateCommand<SampleDataSeries> DeleteSampleCommand { get; set; }

        /// <summary>
        /// 添加图表
        /// </summary>
        public DelegateCommand<object> AddChartCommand { get; set; }

        public DelegateCommand AddCommentCommand { get; set; }
        #endregion

        #region Main

        public ControllerViewModel()
        {
            UpdateDeviceList();
            GlobalValues.TcpClient.StartTcpListen();
            GetFolderFiles();
            var Chart = CreateChart();
            Charts.Add(Chart);

            IsRunning = false;

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
            ShowMenuCommand = new DelegateCommand<object>(ShowMenu);
            DeleteSampleCommand = new DelegateCommand<SampleDataSeries>(DeleteSample);
            AddChartCommand = new DelegateCommand<object>(AddChart);
            AddCommentCommand = new DelegateCommand(AddComment);
        }

        public void InitializeChart()
        {
        }

        /// <summary>
        /// 创建图表
        /// </summary>
        private LightningChart CreateChart()
        {
            var _chart = new LightningChart();

            _chart.PreviewMouseRightButtonDown += (s, e) => e.Handled = true;
            _chart.MouseDoubleClick += Chart_MouseDoubleClick;

            _chart.BeginUpdate();

            _chart.Title.Visible = false;
            _chart.Title.Text = $"chart{_chartCount}";
            _chartCount++;
            Color lineBaseColor = GenerateUniqueColor();

            ViewXY view = _chart.ViewXY;

            view.DropOldEventMarkers = true;
            view.DropOldSeriesData = true;

            DisposeAllAndClear(view.PointLineSeries);
            DisposeAllAndClear(view.YAxes);

            // 设置X轴
            view.XAxes[0].LabelsVisible = true;
            view.XAxes[0].ScrollMode = XAxisScrollMode.Scrolling; // 设置X轴范围
            view.XAxes[0].AllowUserInteraction = true;
            view.XAxes[0].AllowScrolling = false;
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
            yAxis.SetRange(-5, 12); // 设置Y轴范围
            yAxis.MajorGrid.Color = Colors.LightGray;
            view.YAxes.Add(yAxis);

            // 设置图例
            view.LegendBoxes[0].Layout = LegendBoxLayout.Vertical;
            view.LegendBoxes[0].Fill.Color = Colors.Transparent;
            view.LegendBoxes[0].Shadow.Color = Colors.Transparent;
            view.LegendBoxes[0].Position = LegendBoxPositionXY.TopRight;
            view.LegendBoxes[0].SeriesTitleMouseMoveOverOn += ControllerViewModel_SeriesTitleMouseMoveOverOn;

            // 设置Y轴
            view.AxisLayout.AxisGridStrips = XYAxisGridStrips.X;
            view.AxisLayout.YAxesLayout = YAxesLayout.Stacked; // 设置Y轴布局
            view.AxisLayout.SegmentsGap = 2; // 设置Y轴间隔
            view.AxisLayout.YAxisAutoPlacement = YAxisAutoPlacement.LeftThenRight; // 设置Y轴标题位置
            view.AxisLayout.YAxisTitleAutoPlacement = true; // 设置Y轴标题自动位置
            view.AxisLayout.AutoAdjustMargins = false; // 设置是否自动调整边距

            CreateAnnotation(_chart);

            Color color = Colors.Black;

            // 创建8条曲线，每条曲线颜色不同
            for (int i = 0; i < _seriseCount; i++)
            {
                // 创建新的曲线
                var series = new SampleDataSeries(view, view.XAxes[0], view.YAxes[0])
                {
                    Title = new SeriesTitle() { Text = $"Curve {i + 1}" }, // 设置曲线标题
                    ScrollModePointsKeepLevel = 1,
                    //PointsType = PointsType.Points,
                    AllowUserInteraction = true,
                    LineStyle = { Color = ChartTools.CalcGradient(GenerateUniqueColor(), Colors.White, 50) },
                    SampleFormat = SampleFormat.SingleFloat
                };

                view.SampleDataSeries.Add(series);
                SineWaves.Add([]);
                CreateAnnotation(_chart);
            }

            //添加光标
            LineSeriesCursor cursor = new(_chart.ViewXY, _chart.ViewXY.XAxes[0])
            {
                Visible = true,
                SnapToPoints = true,
                ValueAtXAxis = 100
            };
            cursor.LineStyle.Color = Color.FromArgb(150, 255, 0, 0);
            cursor.TrackPoint.Color1 = Colors.White;
            _chart.ViewXY.LineSeriesCursors.Add(cursor);
            cursor.PositionChanged += Cursor_PositionChanged;

            // 重置图表缩放
            _chart.ViewXY.ZoomToFit();

            _chart.AfterRendering += Chart_AfterRendering;

            _chart.EndUpdate();
            _chart.SizeChanged += new SizeChangedEventHandler(Chart_SizeChanged);

            return _chart;
        }

        SampleDataSeries sample = new();

        /// <summary>
        /// 移动到图例栏中的曲线标题时获取当前的曲线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ControllerViewModel_SeriesTitleMouseMoveOverOn(object sender, SeriesTitleDeviceMovedEventArgs e)
        {
            sample = e.Series as SampleDataSeries;
        }

        /// <summary>
        /// 双击图表时触发的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Chart_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var chart = sender as LightningChart;

            var title = sample.Title.Text.Split(':');

            DialogResult result = (DialogResult)DXMessageBox.Show($"是否删除{title[0]}曲线?", "提示", MessageBoxButton.YesNo);

            if (result == DialogResult.Yes)
            {
                chart.ViewXY.SampleDataSeries.Remove(sample);
                UpdateCursorResult(chart);
            }

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
            foreach (var Chart in Charts)
            {
                Chart.ViewXY.LegendBoxes[0].Visible = !Chart.ViewXY.LegendBoxes[0].Visible;
            }
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
            var selectItem = obj as DeviceInfoModel;

            var linkIP = Devices.First(d => d.IpAddress == selectItem.IpAddress);

            GlobalValues.UdpClient.IsConnectDevice(linkIP.IpAddress, true);

            UpdateDeviceList();

        }

        /// <summary>
        /// 断开设备
        /// </summary>
        /// <param name="obj"></param>
        private void DisconnectDevice(object obj)
        {
            var selectItem = obj as DeviceInfoModel;

            var linkIP = Devices.First(d => d.IpAddress == selectItem.IpAddress);

            GlobalValues.UdpClient.IsConnectDevice(linkIP.IpAddress, false);

            UpdateDeviceList();
        }

        //float[][] floats = new float[100][];
        //int _frameCount = 0;

        /// <summary>
        /// 更新设备列表
        /// </summary>
        private void UpdateDeviceList()
        {
            GlobalValues.UdpClient.StartUdpListen();
            Devices = GlobalValues.Devices;


            //for (int i = 0; i < floats.Length; i++)
            //{
            //    float[] yValues = new float[1024];
            //    for (int j = 0; j < 1024; j++)
            //    {
            //        yValues[j] = (float)Math.Sin((j + _frameCount) * 0.1) * 10; // 生成正弦波数据
            //    }

            //    floats[i] = yValues;

            //    _frameCount++;
            //}
        }

        /// <summary>
        /// 刷新界面数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            for (int i = 0; i < Charts.Count; i++)
            {
                UpdateSeriesData(Charts[i]);
            }
        }

        /// <summary>
        /// 更新曲线数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateSeriesData(LightningChart chart)
        {
            chart.BeginUpdate();

            Parallel.For(0, _seriseCount, async (i) =>
            {
                if (await GlobalValues.TcpClient.ChannelReader.WaitToReadAsync() && IsRunning)
                {
                    if (GlobalValues.TcpClient.ChannelReader.TryRead(out var data))
                    {
                        if (GlobalValues.TcpClient.ChannelReader.Count >= 8 && data != null)
                        {
                            int index = data.ChannelID;
                            var series = chart.ViewXY.SampleDataSeries[data.ChannelID];

                            SineWaves[index].AddRange(data.Data);
                            PointNums[index] += data.Data.Length;
                            if (PointNums[index] >= 1024)
                            {
                                series.SamplesSingle = [.. SineWaves[index]];
                                //await SaveData(SineWaves[0]);
                                PointNums[index] = 0;
                                SineWaves[index].Clear();
                            }
                        }
                    }
                }
            });

            //SaveData(SineWaves[0]);
            chart.EndUpdate();

            UpdateCursorResult(chart);
        }

        /// <summary>
        /// 保存折线数据
        /// </summary>
        private async Task SaveData(List<float> datas)
        {
            try
            {
                string fullPath = Path.Combine(folderPath, DateTime.Now.ToString("yyyyMMddHHmmss_") + "Curve_1.txt");

                // 使用异步文件流和流写入器
                using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                using var sw = new StreamWriter(fs);
                // 创建一个待写入的字符串列表
                var lines = new List<string>();

                // 填充列表
                for (int i = 0; i < datas.Count; i++)
                {
                    lines.Add($"{i}-{Convert.ToDouble(datas[i])}");
                }

                // 一次性写入所有数据（异步写入）
                await sw.WriteAsync(string.Join(Environment.NewLine, lines));
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
        /// 开始试验
        /// </summary>
        private async Task StartChart()
        {
            if (GlobalValues.TcpClient.client != null)
            {
                await GlobalValues.TcpClient.SendDataClient(1);
            }

            CompositionTarget.Rendering += CompositionTarget_Rendering;

            foreach (var Chart in Charts)
            {
                int count = Chart.ViewXY.PointLineSeries.Count;

                if (count > 8)
                {
                    for (int i = count; i > 8; i--)
                    {
                        Chart.ViewXY.PointLineSeries.Remove(Chart.ViewXY.PointLineSeries[i - 1]);
                        Chart.ViewXY.YAxes.Remove(Chart.ViewXY.YAxes[i - 1]);
                    }
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
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
            }

            IsRunning = false; // 更新运行状态
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

            var chart = e.Cursor.OwnerView.OwnerChart;

            UpdateCursorResult(chart);
        }

        /// <summary>
        /// 根据X值解决Y值
        /// </summary>
        /// <param name="series"></param>
        /// <param name="xValue"></param>
        /// <param name="yValue"></param>
        /// <returns></returns>
        private bool SolveValueAccurate(SampleDataSeries series, double xValue, out double yValue)
        {
            yValue = 0;

            LineSeriesValueSolveResult result = series.SolveYValueAtXValue(xValue);
            if (result.SolveStatus == LineSeriesSolveStatus.OK)
            {
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
            var chart = sender as LightningChart;
            UpdateCursorResult(chart);
        }

        private void Chart_AfterRendering(object sender, AfterRenderingEventArgs e)
        {
            var chart = sender as LightningChart;
            chart.AfterRendering -= Chart_AfterRendering;
            UpdateCursorResult(chart);
        }

        /// <summary>
        /// 更新光标结果
        /// </summary>
        public void UpdateCursorResult(LightningChart Chart)
        {
            Chart.BeginUpdate();

            //获取光标
            LineSeriesCursor cursor = Chart.ViewXY.LineSeriesCursors[0];

            //cursor.GetCurrentValueAsFrozen

            //获取注释
            List<AnnotationXY> cursorValues = Chart.ViewXY.Annotations;

            float targetYCoord = (float)Chart.ViewXY.GetMarginsRect().Bottom;
            //float targetYCoord = 675;
            Chart.ViewXY.YAxes[0].CoordToValue(targetYCoord, out double y);

            cursorValues[0].TargetAxisValues.X = cursor.ValueAtXAxis;
            cursorValues[0].TargetAxisValues.Y = y;

            StringBuilder sb = new();
            StringBuilder sc = new();
            int seriesNumber = 1;

            string value;

            foreach (var series in Chart.ViewXY.SampleDataSeries)
            {
                //如果批注中的光标值没有显示在光标旁边，则在图表的右侧显示其中的系列标题和光标值
                string title = series.Title.Text.Split(':')[0];
                bool resolvedOK = false;

                resolvedOK = SolveValueAccurate(series, cursor.ValueAtXAxis, out double seriesYValue);

                AxisY axisY = Chart.ViewXY.YAxes[series.AssignYAxisIndex];

                value = string.Format("{0}: {1,12:#####.###} {2}", title, seriesYValue.ToString("0.0"), axisY.Units.Text);

                cursorValues[seriesNumber].TargetAxisValues.X = cursor.ValueAtXAxis;
                cursorValues[seriesNumber].TargetAxisValues.Y = seriesYValue;
                cursorValues[seriesNumber].Visible = true;
                cursorValues[seriesNumber].Text = Math.Round(seriesYValue, 2).ToString();

                sb.AppendLine(value);
                series.Title.Text = value;
                seriesNumber++;
            }

            //sb.AppendLine("");
            sc.AppendLine("X: " + Math.Round(cursor.ValueAtXAxis, 2).ToString());

            //cursorValueDisplay.TargetScreenCoords = new PointDoubleXY(cursor.ValueAtXAxis, 0);
            //设置文本
            cursorValues[0].Text = sc.ToString();
            cursorValues[0].Visible = true;
            Chart.EndUpdate();
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

        /// <summary>
        /// 右键删除曲线
        /// </summary>
        /// <param name="sample"></param>
        private void DeleteSample(SampleDataSeries sample)
        {
            var chart = sample.OwnerView.OwnerChart;
            chart.ViewXY.SampleDataSeries.Remove(sample);
            UpdateCursorResult(chart);
        }

        /// <summary>
        /// 显示右键菜单
        /// </summary>
        private void ShowMenu(object obj)
        {
            var canvas = obj as Canvas;

            var chart = canvas.Children[0] as LightningChart;

            ContextMenu menu = new();

            if (sample != null)
            {
                MenuItem menuItem = new()
                {
                    Header = "删除曲线",
                    Command = DeleteSampleCommand,
                    CommandParameter = sample
                };
                menu.Items.Add(menuItem);

                chart.ContextMenu = menu;
            }

            // 初始化图表事件处理
            chart.MouseRightButtonDown += (s, e) => e.Handled = true;

        }

        /// <summary>
        /// 重置图表缩放
        /// </summary>
        private void ZoomToFitChart()
        {
            foreach (var Chart in Charts)
            {
                Chart.ViewXY.ZoomToFit();
            }
        }

        /// <summary>
        /// 创建注释
        /// </summary>
        /// <param name="y"></param>
        public void CreateAnnotation(LightningChart chart)
        {
            //添加注释以显示游标值
            AnnotationXY annot = new(chart.ViewXY, chart.ViewXY.XAxes[0], chart.ViewXY.YAxes[0])
            {
                Style = AnnotationStyle.Rectangle,
                LocationCoordinateSystem = CoordinateSystem.RelativeCoordinatesToTarget,
            };
            annot.LocationRelativeOffset.X = 50;
            annot.LocationRelativeOffset.Y = 0;
            annot.Sizing = AnnotationXYSizing.Automatic;
            annot.TextStyle.Color = Colors.Black;
            annot.Text = "";
            annot.AllowTargetMove = false;
            annot.AllowAnchorAdjust = false;
            annot.AllowRotate = false;
            annot.AllowResize = false;
            annot.Fill.Color = Colors.White;
            annot.Fill.GradientColor = Colors.Gray;
            annot.BorderVisible = false;
            annot.AllowUserInteraction = false;
            annot.Visible = false;
            chart.ViewXY.Annotations.Add(annot);
        }

        LayoutGroup layoutGroup = new();

        ResizableTextBox text = new();

        /// <summary>
        /// 添加图表
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AddChart(object obj)
        {
            layoutGroup = obj as LayoutGroup;

            if (layoutGroup != null)
            {
                var layPanel = new LayoutPanel();
                var canvas = new Canvas();
                var chart = CreateChart();
                Charts.Add(chart);

                layPanel.AllowDrop = true;
                layPanel.Drop += (o, e) =>
                {
                    if (e.Data.GetDataPresent(System.Windows.DataFormats.StringFormat))
                    {
                        string data = e.Data.GetData(System.Windows.DataFormats.StringFormat) as string;

                        string filePath = Path.Combine("D:\\Datas", data);

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
                                chart.BeginUpdate();

                                SampleDataSeries series = new(chart.ViewXY, chart.ViewXY.XAxes[0], chart.ViewXY.YAxes[0])
                                {
                                    Title = new SeriesTitle() { Text = data }, // 设置曲线标题
                                    LineStyle = { Color = ChartTools.CalcGradient(GenerateUniqueColor(), Colors.White, 50), },
                                    SampleFormat = SampleFormat.SingleFloat
                                };

                                series.MouseDoubleClick += (s, e) =>
                                {
                                    var title = series.Title.Text.Split(':');

                                    DialogResult result = (DialogResult)DXMessageBox.Show($"是否删除{title[0]}曲线?", "提示", MessageBoxButton.YesNo);

                                    if (result == DialogResult.Yes)
                                    {
                                        chart.ViewXY.SampleDataSeries.Remove(series);
                                        UpdateCursorResult(chart);
                                    }
                                };

                                series.AddSamples(YDatas, false);

                                chart.ViewXY.SampleDataSeries.Add(series);
                                chart.ViewXY.LineSeriesCursors[0].Visible = true;
                                CreateAnnotation(chart);
                                chart.EndUpdate();

                                UpdateCursorResult(chart);
                            }
                        }

                    }
                };

                canvas.PreviewMouseDown += (o, e) =>
                {
                    var hitTest = VisualTreeHelper.HitTest(canvas, e.GetPosition(canvas));

                    if (hitTest == null || hitTest.VisualHit == canvas)
                    {
                        text.ClearFocus();
                    }
                };
                canvas.SizeChanged += (o, e) =>
                {
                    chart.Width = canvas.ActualWidth;
                    chart.Height = canvas.ActualHeight;
                };
                canvas.PreviewMouseRightButtonDown += (o, e) =>
                {
                    ShowMenu(canvas);
                };

                canvas.Children.Add(chart);
                layPanel.Content = canvas;
                layoutGroup.Items.Add(layPanel);
            }
        }

        /// <summary>
        /// 添加注释
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void AddComment()
        {
            foreach (var item in layoutGroup.Items)
            {
                var panel = item as LayoutPanel;

                var canvas = panel.Content as Canvas;
                var text = new ResizableTextBox()
                {
                    Width = 200,
                    Height = 100,
                };

                Canvas.SetLeft(text, 50);
                Canvas.SetTop(text, 50);

                canvas.Children.Add(text);
            }
        }
        #endregion
    }
}
