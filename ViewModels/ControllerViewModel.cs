using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Annotations;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.SeriesXY;
using Arction.Wpf.Charting.Titles;
using Arction.Wpf.Charting.Views.ViewXY;
using CotrollerDemo.Models;
using CotrollerDemo.Views;
using DevExpress.Mvvm.Native;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Annotations;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

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
            get => _fileNames;
            set => SetProperty(ref _fileNames, value);
        }

        private ObservableCollection<DeviceInfoModel> _devices = [];

        /// <summary>
        /// 设备列表
        /// </summary>
        public ObservableCollection<DeviceInfoModel> Devices
        {
            get => _devices;
            set => SetProperty(ref _devices, value);
        }

        //public LightningChart Chart = new();

        public List<LightningChart> Charts { get; set; } = [];

        private List<SeriesPoint[]> _seriesPoints = [];

        public List<SeriesPoint[]> SeriesPoints
        {
            get => _seriesPoints;
            set => SetProperty(ref _seriesPoints, value);
        }

        private int _chartCount = 1;

        /// <summary>
        /// 曲线数量
        /// </summary>
        private readonly int _seriesCount = 8;

        /// <summary>
        /// 存放已生成的颜色
        /// </summary>
        private static readonly HashSet<Color> GeneratedColors = [];

        // 存放路径s
        public string FolderPath = @"D:\Datas";

        private bool _isRunning;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
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
            get => _isDrop;
            set => SetProperty(ref _isDrop, value);
        }

        /// <summary>
        /// 正弦波数据
        /// </summary>
        public List<List<float>> SineWaves { get; set; } = [];

        /// <summary>
        /// 曲线点数数量
        /// </summary>
        public int[] PointNumbers = new int[8];

        private readonly ConcurrentDictionary<int, List<float>> _dataBuffer = new();
        //private readonly ConcurrentQueue<List<float>> _fileData = new();
        private readonly ConcurrentQueue<float[][]> _fileData = new();

        public SampleDataSeries Sample { get; set; } = new();

        public Thread _executeThread = null;

        #endregion Property

        #region Command

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

        #endregion Command

        #region Main

        public ControllerViewModel()
        {
            _executeThread = new Thread(UpdateSeriesData);
            UpdateDeviceList();
            GlobalValues.TcpClient.StartTcpListen();
            GetFolderFiles();
            var chart = CreateChart();
            Charts.Add(chart);
            SaveData();

            IsRunning = false;

            StartTestCommand = new AsyncDelegateCommand(StartTest, CanStartChart).ObservesProperty(() => IsRunning);
            StopTestCommand = new AsyncDelegateCommand(StopTest, CanStopChart).ObservesProperty(() => IsRunning);
            OpenFolderCommand = new DelegateCommand(OpenFolder);
            DeviceQueryCommand = new DelegateCommand(UpdateDeviceList);
            ClearFolderCommand = new DelegateCommand(ClearFolder);
            ConnectCommand = new DelegateCommand<object>(ConnectDevice);
            DisconnectCommand = new DelegateCommand<object>(DisconnectDevice);
            SwitchLegendCommand = new DelegateCommand<object>(SwitchLegend);
            DeleteFileCommand = new DelegateCommand<object>(DeleteFile);
            ShowMenuCommand = new DelegateCommand<object>(ShowMenu);
            DeleteSampleCommand = new DelegateCommand<SampleDataSeries>(DeleteSample);
            AddChartCommand = new DelegateCommand<object>(AddChart);
        }

        /// <summary>
        /// 创建图表
        /// </summary>
        private LightningChart CreateChart()
        {
            var chart = new LightningChart();

            chart.PreviewMouseRightButtonDown += (s, e) => e.Handled = true;

            chart.BeginUpdate();

            chart.Title.Visible = false;
            chart.Title.Text = $"chart{_chartCount}";
            _chartCount++;
            var lineBaseColor = GenerateUniqueColor();

            var view = chart.ViewXY;

            view.DropOldEventMarkers = true;
            view.DropOldSeriesData = true;

            DisposeAllAndClear(view.PointLineSeries);
            DisposeAllAndClear(view.YAxes);

            // 设置X轴
            view.XAxes[0].LabelsVisible = true;
            view.XAxes[0].ScrollMode = XAxisScrollMode.Scrolling;
            view.XAxes[0].AllowUserInteraction = true;
            view.XAxes[0].AllowScrolling = false;
            view.XAxes[0].SetRange(0, 1024);
            view.XAxes[0].ValueType = AxisValueType.Number;
            view.XAxes[0].AutoFormatLabels = false;
            view.XAxes[0].LabelsNumberFormat = "N0";
            view.XAxes[0].MajorGrid.Pattern = LinePattern.Solid;
            view.XAxes[0].Title = null;
            view.XAxes[0].MajorGrid.Visible = false;
            view.XAxes[0].MinorGrid.Visible = false;

            // 设置Y轴
            var yAxis = new AxisY(view);
            yAxis.Title.Text = null;
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
            yAxis.SetRange(-5, 10); // 调整Y轴范围为正常波形范围
            yAxis.MajorGrid.Color = Colors.LightGray;
            view.YAxes.Add(yAxis);

            // 设置图例
            view.LegendBoxes[0].Layout = LegendBoxLayout.Vertical;
            view.LegendBoxes[0].Fill.Color = Colors.Transparent;
            view.LegendBoxes[0].Shadow.Color = Colors.Transparent;
            view.LegendBoxes[0].Position = LegendBoxPositionXY.TopRight;
            view.LegendBoxes[0].SeriesTitleMouseMoveOverOn += ControllerViewModel_SeriesTitleMouseMoveOverOn;

            // 设置Y轴布局
            view.AxisLayout.AxisGridStrips = XYAxisGridStrips.X;
            view.AxisLayout.YAxesLayout = YAxesLayout.Stacked;
            view.AxisLayout.SegmentsGap = 2;
            view.AxisLayout.YAxisAutoPlacement = YAxisAutoPlacement.LeftThenRight;
            view.AxisLayout.YAxisTitleAutoPlacement = true;
            view.AxisLayout.AutoAdjustMargins = false;

            CreateAnnotation(chart);

            // 创建8条曲线，每条曲线颜色不同
            for (int i = 0; i < _seriesCount; i++)
            {
                var series = new SampleDataSeries(view, view.XAxes[0], view.YAxes[0])
                {
                    Title = new SeriesTitle() { Text = $"CH {i + 1}" },
                    ScrollModePointsKeepLevel = 1,
                    AllowUserInteraction = true,
                    LineStyle = { Color = ChartTools.CalcGradient(GenerateUniqueColor(), Colors.White, 50) },
                    SampleFormat = SampleFormat.SingleFloat
                };

                series.MouseOverOn += (_, _) =>
                {
                    Sample = series;
                };

                SineWaves.Add([]);

                view.SampleDataSeries.Add(series);
                CreateAnnotation(chart);
            }

            CreateLineSeriesCursor(chart, true);
            chart.ViewXY.ZoomToFit();
            chart.AfterRendering += Chart_AfterRendering;
            chart.EndUpdate();
            chart.SizeChanged += new SizeChangedEventHandler(Chart_SizeChanged);

            return chart;
        }

        /// <summary>
        /// 移动到图例栏中的曲线标题时获取当前的曲线
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ControllerViewModel_SeriesTitleMouseMoveOverOn(object sender, SeriesTitleDeviceMovedEventArgs e)
        {
            lock (Sample)
            {
                Sample = e.Series as SampleDataSeries;
            }
        }

        /// <summary>
        /// 切换图例状态
        /// </summary>
        /// <param name="obj"></param>
        private void SwitchLegend(object obj)
        {
            if (obj is SimpleButton btn)
                btn.Content = btn.Content.ToString() == "隐藏图例" ? "显示图例" : "隐藏图例";

            foreach (var chart in Charts)
            {
                chart.ViewXY.LegendBoxes[0].Visible = !chart.ViewXY.LegendBoxes[0].Visible;
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

            var linkIp = Devices.First(d => selectItem != null && Equals(d.IpAddress, selectItem.IpAddress));

            GlobalValues.UdpClient.IsConnectDevice(linkIp.IpAddress, true);

            UpdateDeviceList();
        }

        /// <summary>
        /// 断开设备
        /// </summary>
        /// <param name="obj"></param>
        private void DisconnectDevice(object obj)
        {
            var selectItem = obj as DeviceInfoModel;

            var linkIp = Devices.First(d => selectItem != null && Equals(d.IpAddress, selectItem.IpAddress));

            GlobalValues.UdpClient.IsConnectDevice(linkIp.IpAddress, false);

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
            /*
              for (int i = 0; i < floats.Length; i++)
             {
                 float[] yValues = new float[1024];
                 for (int j = 0; j < 1024; j++)
                 {
                     yValues[j] = (float)Math.Sin((j + _frameCount) * 0.1) * 10; // 生成正弦波数据
                 }

                 floats[i] = yValues;

                 _frameCount++;
            }*/
        }

        /// <summary>
        /// 刷新界面数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            UpdateSeriesData();
        }

        /// <summary>
        /// 更新曲线数据
        /// </summary>
        private void UpdateSeriesData()
        {
            if (!IsRunning) return;

            try
            {
                // 使用TryRead而不是WaitToReadAsync，避免阻塞
                while (GlobalValues.TcpClient.ChannelReader.TryRead(out var data))
                {
                    if (data == null) continue;

                    var channelId = data.ChannelId;
                    _dataBuffer.AddOrUpdate(channelId, _ => [.. data.Data], (_, list) => { list.AddRange(data.Data); return list; });

                    Interlocked.Add(ref PointNumbers[channelId], data.Data.Length);
                }

                // 检查是否所有通道都达到了1024点
                bool allChannelsReady = PointNumbers.All(count => count >= 1024);
                if (!allChannelsReady) return;

                // 准备所有通道的数据
                var channelData = new float[_seriesCount][];
                for (int i = 0; i < _seriesCount; i++)
                {
                    if (_dataBuffer.TryGetValue(i, out var buffer) && buffer.Count >= 1024)
                    {
                        channelData[i] = [.. buffer.Take(1024)];
                        // 移除已处理的数据
                        buffer.RemoveRange(0, 1024);
                        Interlocked.Add(ref PointNumbers[i], -1024);
                    }
                }

                // 使用UI线程一次性更新所有图表
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var chart in Charts)
                    {
                        try
                        {
                            chart.BeginUpdate();
                            for (int i = 0; i < _seriesCount; i++)
                            {
                                if (channelData[i] != null)
                                {
                                    var series = chart.ViewXY.SampleDataSeries[i];
                                    series.SamplesSingle = channelData[i];
                                }
                            }

                            // 控制光标更新频率
                            UpdateCursorResult(chart);

                            chart.EndUpdate();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error updating chart: {ex.Message}");
                        }
                    }

                    _fileData.Enqueue(channelData);
                });

                Task.Delay(25).Wait();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateSeriesData: {ex.Message}");
            }
        }


        /// <summary>
        /// </summary>
        /// 保存折线数据
        private void SaveData()
        {
            Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (_fileData.TryDequeue(out var data) && data.Length > 0)
                        {
                            for (int i = 0; i < data.Length; i++)
                            {
                                string fullPath = Path.Combine(FolderPath, DateTime.Now.ToString("yyyyMMddHHmmssff_") + $"CH{i + 1}.txt");

                                // 使用异步文件流和流写入器
                                await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
                                await using var sw = new StreamWriter(fs);
                                // 创建一个待写入的字符串列表
                                var lines = new List<string>();

                                // 填充列表
                                for (int j = 0; j < data[i].Length; j++)
                                {
                                    lines.Add($"{j}-{Convert.ToDouble(data[i][j])}");
                                }

                                // 一次性写入所有数据（异步写入）
                                await sw.WriteAsync(string.Join(Environment.NewLine, lines));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("发生错误: " + ex.Message);
                }
            });
        }

        /// <summary>
        /// 获取文件夹下的所有文件
        /// </summary>
        private void GetFolderFiles()
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                FileNames.Clear();

                string[] files = Directory.GetFiles(FolderPath);

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
        private async Task StartTest()
        {
            if (GlobalValues.TcpClient.Client != null)
            {
                await GlobalValues.TcpClient.SendDataClient(1);
            }

            CompositionTarget.Rendering += CompositionTarget_Rendering;


            foreach (var chart in Charts)
            {
                int count = chart.ViewXY.PointLineSeries.Count;

                if (count > 8)
                {
                    for (int i = count; i > 8; i--)
                    {
                        chart.ViewXY.PointLineSeries.Remove(chart.ViewXY.PointLineSeries[i - 1]);
                        chart.ViewXY.YAxes.Remove(chart.ViewXY.YAxes[i - 1]);
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
            if (GlobalValues.TcpClient.Client != null)
            {
                await GlobalValues.TcpClient.SendDataClient(0);
            }
            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            IsRunning = false; // 更新运行状态
            GetFolderFiles();
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
        private static bool SolveValueAccurate(SampleDataSeries series, double xValue, out double yValue)
        {
            yValue = 0;

            var result = series.SolveYValueAtXValue(xValue);
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
            if (sender is not LightningChart chart) return;
            chart.AfterRendering -= Chart_AfterRendering;
            UpdateCursorResult(chart);
        }

        /// <summary>
        /// 更新光标结果
        /// </summary>
        public void UpdateCursorResult(LightningChart chart)
        {
            chart.BeginUpdate();

            //获取光标
            //var cursor = chart.ViewXY.LineSeriesCursors[0];

            //获取注释
            var cursorValues = chart.ViewXY.Annotations;

            var targetYCoord = (float)chart.ViewXY.GetMarginsRect().Bottom - 20;
            chart.ViewXY.YAxes[0].CoordToValue(targetYCoord, out var y);

            // 更新每条曲线的注释
            var cursors = chart.ViewXY.LineSeriesCursors;

            for (int i = 0; i < cursors.Count; i++)
            {
                int cursorIndex;
                if (i > 0)
                {
                    cursorIndex = _seriesCount * i + 1;
                }
                else
                {
                    cursorIndex = 0;
                }
                int seriesNumber = 1;
                foreach (var series in chart.ViewXY.SampleDataSeries)
                {
                    var title = series.Title.Text.Split(':')[0];
                    if (SolveValueAccurate(series, cursors[i].ValueAtXAxis, out double seriesYValue))
                    {
                        var annotation = cursorValues[seriesNumber + cursorIndex];

                        // 设置注释位置
                        annotation.AxisValuesBoundaries.XMax = Charts.Count > 1 ? cursors[i].ValueAtXAxis + (title.Length + seriesYValue.ToString(CultureInfo.InvariantCulture).Length) * 5.5 : cursors[i].ValueAtXAxis + 50;
                        annotation.AxisValuesBoundaries.XMin = cursors[i].ValueAtXAxis + 10;
                        annotation.AxisValuesBoundaries.YMax = seriesYValue + 0.25;
                        annotation.AxisValuesBoundaries.YMin = seriesYValue - 0.25;

                        // 设置注释内容
                        annotation.Text = $"{title}: {Math.Round(seriesYValue, 2)}";
                        annotation.Visible = true;

                        // 更新曲线标题
                        series.Title.Text = $"{title}: {Math.Round(seriesYValue, 2)}";
                    }
                    seriesNumber++;
                }

                // 设置X轴注释位置和内容

                cursorValues[cursorIndex].AxisValuesBoundaries.XMax = cursors[i].ValueAtXAxis + 45;
                cursorValues[cursorIndex].AxisValuesBoundaries.XMin = cursors[i].ValueAtXAxis + 15;
                cursorValues[cursorIndex].AxisValuesBoundaries.YMax = y + 0.5;
                cursorValues[cursorIndex].AxisValuesBoundaries.YMin = y;
                cursorValues[cursorIndex].Visible = true;
                cursorValues[cursorIndex].Text = $"X: {Math.Round(cursors[i].ValueAtXAxis, 2)}";
            }

            chart.EndUpdate();
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
                // ReSharper disable once SuggestVarOrType_SimpleTypes
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
            } while (GeneratedColors.Contains(color));

            GeneratedColors.Add(color);
            return color;
        }

        /// <summary>
        /// 打开文件夹
        /// </summary>
        private void OpenFolder()
        {
            try
            {
                Process.Start("explorer.exe", FolderPath);
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
                    if (!Directory.Exists(FolderPath))
                    {
                        throw new DirectoryNotFoundException($"文件夹不存在: {FolderPath}");
                    }

                    // 获取文件夹中的所有文件
                    string[] files = Directory.GetFiles(FolderPath);

                    // 删除每个文件
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }

                    // 获取文件夹中的所有子文件夹
                    string[] subFolders = Directory.GetDirectories(FolderPath);

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
                    if (obj is string fileName)
                    {
                        string file = Path.Combine(FolderPath, fileName);
                        File.Delete(file);
                    }

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

            var title = sample.Title.Text.Split(':')[0];

            if (title.Contains('_'))
            {
                if (chart.ViewXY.Annotations.Any(t => t.Text.Contains(title)))
                {
                    var ann = chart.ViewXY.Annotations.First(t => t.Text.Contains(title));
                    chart.ViewXY.SampleDataSeries.Remove(sample);
                    chart.ViewXY.Annotations.Remove(ann);
                    UpdateCursorResult(chart);
                    Sample = new SampleDataSeries();
                }
            }
        }

        /// <summary>
        /// 显示右键菜单
        /// </summary>
        private void ShowMenu(object obj)
        {
            if (obj is not Canvas canvas) return;
            var chart = canvas.Children[0] as LightningChart;

            string title = Sample.Title.Text.Split(":")[0];

            ContextMenu menu = new();

            if (Sample != null && title != "Series title" && title.Length > 4)
            {
                MenuItem deleteItem = new()
                {
                    Header = "删除曲线",
                    Command = DeleteSampleCommand,
                    CommandParameter = Sample
                };
                menu.Items.Add(deleteItem);
            }

            menu.Items.Add(new MenuItem()
            {
                Header = "添加注释",
                Command = new DelegateCommand(() => AddComment(canvas))
            });

            menu.Items.Add(new MenuItem()
            {
                Header = "添加光标",
                Command = new DelegateCommand(() =>
                {
                    CreateLineSeriesCursor(chart, false);
                }),
            });

            if (chart == null) return;

            chart.ContextMenu = menu;
            // 初始化图表事件处理
            chart.MouseRightButtonDown += (s, e) => e.Handled = true;
        }

        /// <summary>
        /// 添加光标
        /// </summary>
        /// <param name="chart"></param>
        /// <param name="isCreateChart">是否是添加图表</param>
        public void CreateLineSeriesCursor(LightningChart chart, bool isCreateChart)
        {
            //添加光标
            LineSeriesCursor cursor = new(chart.ViewXY, chart.ViewXY.XAxes[0])
            {
                Visible = true,
                SnapToPoints = true,
                ValueAtXAxis = 100
            };
            cursor.LineStyle.Color = Color.FromArgb(150, 255, 0, 0);
            cursor.TrackPoint.Color1 = Colors.White;
            cursor.PositionChanged += Cursor_PositionChanged;
            chart.ViewXY.LineSeriesCursors.Add(cursor);

            if (!isCreateChart)
            {
                for (int i = 0; i < chart.ViewXY.SampleDataSeries.Count + 1; i++)
                {
                    CreateAnnotation(chart);
                }
                UpdateCursorResult(chart);
            }
        }

        /// <summary>
        /// 创建注释
        /// </summary>
        /// <param name="chart"></param>
        public void CreateAnnotation(LightningChart chart)
        {
            //添加注释以显示游标值
            AnnotationXY annot = new(chart.ViewXY, chart.ViewXY.XAxes[0], chart.ViewXY.YAxes[0])
            {
                Style = AnnotationStyle.Rectangle,
                LocationCoordinateSystem = CoordinateSystem.RelativeCoordinatesToTarget,
                LocationRelativeOffset = new PointDoubleXY(60, 0),
                Sizing = AnnotationXYSizing.AxisValuesBoundaries,
                AxisValuesBoundaries = new BoundsDoubleXY(0, 0, -5, -4.5)
            };
            annot.TextStyle.Color = Colors.White;
            annot.TextStyle.Font = new WpfFont("Segoe UI", 10);

            annot.AllowTargetMove = false;
            annot.AllowAnchorAdjust = false;
            annot.AllowRotate = false;
            annot.AllowResize = false;

            annot.Fill.Color = Color.FromArgb(200, 255, 255, 255);
            annot.Fill.GradientFill = GradientFill.Solid;
            annot.Fill.GradientColor = Color.FromArgb(200, 255, 255, 255);

            annot.BorderLineStyle.Color = Colors.Transparent;
            annot.BorderLineStyle.Width = 0;
            annot.BorderLineStyle.Pattern = LinePattern.Solid;
            annot.BorderVisible = true;

            annot.AllowUserInteraction = false;
            annot.Visible = false;

            chart.ViewXY.Annotations.Add(annot);
        }

        private LayoutGroup _layoutGroup = new();

        private readonly ResizableTextBox _text = new();

        /// <summary>
        /// 添加图表
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AddChart(object obj)
        {
            _layoutGroup = obj as LayoutGroup;

            if (_layoutGroup != null)
            {
                var layPanel = new LayoutPanel();
                var canvas = new Canvas();
                var chart = CreateChart();

                // 初始化新图表的数据
                if (IsRunning)
                {
                    chart.BeginUpdate();
                    try
                    {
                        // 从_dataBuffer同步当前数据
                        for (int i = 0; i < _seriesCount; i++)
                        {
                            var series = chart.ViewXY.SampleDataSeries[i];
                            // ReSharper disable once InvertIf
                            if (_dataBuffer.TryGetValue(i, out var buffer) && buffer.Count >= 1024)
                            {
                                var data = buffer.Take(1024).ToArray();
                                series.SamplesSingle = data;
                            }
                        }
                        UpdateCursorResult(chart);
                    }
                    finally
                    {
                        chart.EndUpdate();
                    }
                }

                // 添加到Charts集合要在数据初始化之后
                Charts.Add(chart);

                // 添加关闭事件处理
                layPanel.CloseCommand = new DelegateCommand(() =>
                {
                    // 从Charts集合中移除图表
                    if (Charts != null && Charts.Contains(chart))
                    {
                        Charts.Remove(chart);
                        // 释放图表资源
                        chart.Dispose();
                    }
                    layPanel.Closed = true;
                });

                layPanel.ContextMenuCustomizations.AddRange(_layoutGroup.Items[0].ContextMenuCustomizations);
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
                            var lines = File.ReadAllLines(filePath);
                            var datas = new string[lines.Length][];
                            var yDatas = new float[lines.Length];

                            for (int i = 0; i < lines.Length; i++)
                            {
                                datas[i] = lines[i].Split(['-'], 2, StringSplitOptions.RemoveEmptyEntries);
                                yDatas[i] = Convert.ToSingle(datas[i][1]);
                            }

                            chart.BeginUpdate();

                            SampleDataSeries series = new(chart.ViewXY, chart.ViewXY.XAxes[0], chart.ViewXY.YAxes[0])
                            {
                                Title = new SeriesTitle() { Text = data },
                                LineStyle = { Color = ChartTools.CalcGradient(GenerateUniqueColor(), Colors.White, 50), },
                                SampleFormat = SampleFormat.SingleFloat
                            };

                            series.MouseOverOn += (sender, args) =>
                            {
                                Sample = series;
                            };

                            series.AddSamples(yDatas, false);

                            CreateAnnotation(chart);
                            chart.ViewXY.SampleDataSeries.Add(series);
                            chart.ViewXY.LineSeriesCursors[0].Visible = true;

                            chart.EndUpdate();

                            UpdateCursorResult(chart);
                        }
                    }
                };

                canvas.PreviewMouseDown += (o, e) =>
                {
                    var hitTest = VisualTreeHelper.HitTest(canvas, e.GetPosition(canvas));

                    if (hitTest == null || hitTest.VisualHit == canvas)
                    {
                        _text.ClearFocus();
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
                _layoutGroup.Items.Add(layPanel);
            }
        }

        /// <summary>
        /// 添加注释
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void AddComment(Canvas canvas)
        {
            var text = new ResizableTextBox()
            {
                Width = 200,
                Height = 100,
            };

            Canvas.SetLeft(text, 50);
            Canvas.SetTop(text, 50);

            canvas.Children.Add(text);
        }

        #endregion Main
    }
}