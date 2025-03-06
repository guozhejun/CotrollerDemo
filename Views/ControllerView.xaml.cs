using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.SeriesXY;
using DevExpress.Xpf.Core.ConditionalFormattingManager;
using System;
using System.Collections.Generic;
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
            CreateChart();
        }


        private void CreateChart()
        {
            _chart = new LightningChart();

            _chart.BeginUpdate();

            //隐藏图例框
            _chart.ViewXY.LegendBoxes[0].Visible = false;

            _chart.Title.Visible = false;

            // 设置X轴
            AxisX axisX = _chart.ViewXY.XAxes[0];
            axisX.SetRange(0, 20);
            axisX.ScrollMode = XAxisScrollMode.Scrolling;
            axisX.Title.Visible = false;
            axisX.AutoDivSpacing = false;
            axisX.MajorDiv = 2;
            axisX.ValueType = AxisValueType.Number;
            axisX.LabelsNumberFormat = "0";

            // 设置Y轴
            AxisY axisY = _chart.ViewXY.YAxes[0];
            axisY.SetRange(0, 100);
            axisY.LabelsNumberFormat = "0";
            axisY.AutoFormatLabels = false;

            // 生成随机数据
            Random random = new Random();
            const double interval = 1;
            int pointsCount = (int)((axisX.Maximum - axisX.Minimum) / interval) + 1;

            SeriesPoint[] points = new SeriesPoint[pointsCount];
            for (int pointIndex = 0; pointIndex < pointsCount; pointIndex++)
            {
                points[pointIndex].X = pointIndex;
                points[pointIndex].Y = 100.0 * random.NextDouble();
            }

            // 添加点到线
            PointLineSeries pointLineSeries = new PointLineSeries(_chart.ViewXY, axisX, _chart.ViewXY.YAxes[0])
            {
                PointsVisible = true,
                Points = points,
            };

            _chart.ViewXY.PointLineSeries.Add(pointLineSeries);


            _chart.EndUpdate();

            canvasBase.Children.Add(_chart);
        }

        private LightningChart _chart;

        private void canvasBase_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _chart.Width = lightingChartItem.ActualWidth;
            _chart.Height = lightingChartItem.ActualHeight;
        }
    }
}
