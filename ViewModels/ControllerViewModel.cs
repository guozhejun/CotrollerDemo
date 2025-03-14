using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Annotations;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.SeriesXY;
using Arction.Wpf.Charting.Views;
using Arction.Wpf.Charting.Views.ViewXY;
using CotrollerDemo.Models;
using DevExpress.Mvvm.Native;
using DevExpress.Utils;
using DryIoc.ImTools;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        private object _chartContent;
        public object ChartContent
        {
            get { return _chartContent; }
            set { SetProperty(ref _chartContent, value); }
        }

        public static LightningChart chart { get; set; } = new();

        public static ViewXY view { get; set; } = new();

        private List<SeriesPoint[]> _dataSeries = [];

        private int _pointCount = 0;

        private const int MaxPoints = 1024;

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
                SetProperty(ref _isRunning, value);
            }
        }

        public DelegateCommand DeviceSearchCommand { get; set; }

        public DelegateCommand SaveDataCommand { get; set; }

        public DelegateCommand StartTestCommand { get; set; }

        public DelegateCommand StopTestCommand { get; set; }

        public DelegateCommand<object> ConnectCommand { get; set; }

        public DelegateCommand<object> DisconnectCommand { get; set; }

        public ControllerViewModel()
        {
            CreateChart();

            for (int i = 0; i < 8; i++)
            {
                _dataSeries.Add(new SeriesPoint[MaxPoints]);
            }

            ChartContent = chart;

            UpdateDeviceList();

            StartTestCommand = new DelegateCommand(StartChart, CanStartChart).ObservesProperty(() => IsRunning);
            StopTestCommand = new DelegateCommand(StopTest, CanStopChart).ObservesProperty(() => IsRunning);
            SaveDataCommand = new DelegateCommand(SaveData);
            ConnectCommand = new DelegateCommand<object>(ConnectDevice);
            DisconnectCommand = new DelegateCommand<object>(DisconnectDevice);
            GetFolderFiles();
        }

        private bool CanStopChart()
        {
            return IsRunning;
        }

        private bool CanStartChart()
        {
            return !IsRunning;
        }

        private void ConnectDevice(object obj)
        {
            var selectItem = obj as DeviceInfoModel;

            var linkIP = GlobalValues.DeviceList.First(d => d.IpEndPoint.Address == selectItem.IpAddress);

            GlobalValues.TcpClient.tcp.Start();

            Devices.Clear();

            GlobalValues.UdpClient.IsConnectDevice(linkIP.IpEndPoint.Address, true);

            UpdateDeviceList();
        }

        private void DisconnectDevice(object obj)
        {
            var selectItem = obj as DeviceInfoModel;

            var linkIP = GlobalValues.DeviceList.First(d => d.IpEndPoint.Address == selectItem.IpAddress);

            Devices.Clear();

            GlobalValues.UdpClient.IsConnectDevice(linkIP.IpEndPoint.Address, false);

            GlobalValues.TcpClient.client?.Close();
            GlobalValues.TcpClient.stream?.Close();
            GlobalValues.TcpClient.tcp?.Stop();

            UpdateDeviceList();
        }

        private void UpdateDeviceList()
        {
            Devices.Clear();
            GlobalValues.DeviceList.ForEach(d =>
            {
                Devices.Add(new()
                {
                    IpAddress = d.IpEndPoint.Address,
                    SerialNum = d.SerialNum,
                    Status = d.Status == 1 ? "已连接" : "未连接"
                });
            });
        }

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

        private void CreateChart()
        {
            chart.BeginUpdate();

            chart.ChartRenderOptions.DeviceType = RendererDeviceType.AutoPreferD11;
            chart.ChartRenderOptions.LineAAType2D = LineAntiAliasingType.QLAA;

            view = chart.ViewXY;

            view.XAxes[0].ScrollMode = XAxisScrollMode.Scrolling; // 设置X轴滚动模式
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
            view.AxisLayout.AxisGridStrips = XYAxisGridStrips.X;
            view.AxisLayout.YAxesLayout = YAxesLayout.Stacked; // 设置Y轴布局
            view.AxisLayout.SegmentsGap = 2; // 设置Y轴间隔
            view.AxisLayout.YAxisAutoPlacement = YAxisAutoPlacement.LeftThenRight; // 设置Y轴标题位置
            view.AxisLayout.YAxisTitleAutoPlacement = true; // 设置Y轴标题自动位置

            view.AxisLayout.AutoAdjustMargins = false; // 设置是否自动调整边距

            chart.EndUpdate();
        }

        private void StartChart()
        {

            //_ = GlobalValues.TcpClient.SendDataClient(1);

            chart.BeginUpdate();

            DisposeAllAndClear(view.PointLineSeries);
            DisposeAllAndClear(view.YAxes);

            // 创建8条曲线，每条曲线颜色不同
            for (int i = 0; i < _dataSeries.Count; i++)
            {

                Color lineBaseColor = GenerateUniqueColor();
                // 创建新的Y轴
                var yAxis = new AxisY(view);
                yAxis.Title.Text = $"Y{i + 1}"; // 设置Y轴标题
                yAxis.Title.Visible = true;
                yAxis.Title.Angle = 0;
                yAxis.Title.Color = lineBaseColor;
                yAxis.Units.Visible = false;
                yAxis.AllowScaling = false;
                yAxis.MajorGrid.Visible = false;
                yAxis.MinorGrid.Visible = false;
                yAxis.MajorGrid.Pattern = LinePattern.Solid;
                yAxis.AutoDivSeparationPercent = 0;
                yAxis.Visible = true;
                yAxis.MajorDivTickStyle.Alignment = Alignment.Near;
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
                    AllowUserInteraction = false,
                    LineStyle = { Color = ChartTools.CalcGradient(lineBaseColor, Colors.White, 50) }
                };

                view.PointLineSeries.Add(series);
            }

            chart.EndUpdate();

            _timer = new()
            {
                Interval = TimeSpan.FromMilliseconds(100), // 更新间隔
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            IsRunning = true; // 更新运行状态
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            PointLineSeries series = new();

            chart.BeginUpdate();


            for (int i = 0; i < view.PointLineSeries.Count; i++)
            {
                double y = random.NextDouble() * 100; // 生成随机数据
                _dataSeries[i][_pointCount] = new SeriesPoint(_pointCount, y);

                series = view.PointLineSeries[i];
                series.Points = _dataSeries[i];

            }

            _pointCount++;

            if (_pointCount >= MaxPoints)
            {
                SaveData();
                _pointCount = 0;
                series.LineStyle = new LineStyle()
                {
                    Color = ChartTools.CalcGradient(GenerateUniqueColor(), Colors.White, 50)
                };
            }

            chart.EndUpdate();
        }

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

        public static Color GenerateUniqueColor()
        {

            Color color;
            do
            {
                byte red = (byte)random.Next(256);
                byte green = (byte)random.Next(256);
                byte blue = (byte)random.Next(256);
                color = Color.FromArgb(255, red, green, blue);
            } while (generatedColors.Contains(color));

            generatedColors.Add(color);
            return color;
        }
    }

}
