﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Arction.Wpf.Charting;
using Arction.Wpf.Charting.SeriesXY;
using CotrollerDemo.ViewModels;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Editors;

namespace CotrollerDemo.Views
{
    /// <summary>
    /// ControllerView.xaml 的交互逻辑
    /// </summary>
    public partial class ControllerView : UserControl
    {
        // 使用类级别的字段跟踪拖拽状态，避免重复触发
        private bool _isDragging = false;

        private List<string> _titleList = [];

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
                // 如果已经在拖拽中，直接返回
                if (_isDragging)
                    return;

                // 检查鼠标左键是否按下
                if (e.LeftButton != MouseButtonState.Pressed)
                    return;

                // 检查发送者是否为ListBoxEdit
                if (sender is not ListBoxEdit listBoxEdit)
                    return;

                // 检查是否有选中的项目
                if (listBoxEdit.SelectedItem == null)
                    return;

                string fileName = listBoxEdit.SelectedItem.ToString();
                if (string.IsNullOrEmpty(fileName))
                    return;

                // 清空标题列表，避免重复添加
                _titleList.Clear();

                // 获取当前图表中所有曲线的标题
                foreach (
                    var title in _controller
                        .Charts[0]
                        .ViewXY.SampleDataSeries.Select(series =>
                            series.Title.Text.Split([':'], 2)[0]
                        )
                )
                {
                    _titleList.Add(title);
                }

                // 检查当前选中的文件是否已经存在于图表中
                string fileTitle = fileName.Split('.')[0];
                if (_titleList.Contains(fileTitle))
                {
                    // 如果文件已经存在，显示提示并返回
                    DXMessageBox.Show(
                        $"文件 {fileName} 已经添加到图表中，不能重复添加。",
                        "提示",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                // 设置拖拽状态
                _isDragging = true;

                try
                {
                    // 使用更安全的方式创建数据对象
                    var data = new DataObject();
                    data.SetData(DataFormats.StringFormat, fileName);

                    // 执行拖拽操作
                    DragDrop.DoDragDrop(listBoxEdit, data, DragDropEffects.Copy);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"拖拽操作出错: {ex.Message}");
                }
                finally
                {
                    // 无论成功与否，都重置拖拽状态
                    _isDragging = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ListBoxEdit_PreviewMouseMove出错: {ex.Message}");
                // 确保拖拽状态被重置
                _isDragging = false;
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

                        SampleDataSeries series = new(
                            _controller.Charts[0].ViewXY,
                            _controller.Charts[0].ViewXY.XAxes[0],
                            _controller.Charts[0].ViewXY.YAxes[0]
                        )
                        {
                            Title = new Arction.Wpf.Charting.Titles.SeriesTitle() { Text = title }, // 设置曲线标题
                            LineStyle =
                            {
                                Color = ChartTools.CalcGradient(
                                    _controller.GenerateUniqueColor(),
                                    Colors.White,
                                    50
                                ),
                            },
                            SampleFormat = SampleFormat.SingleFloat,
                        };

                        series.MouseOverOn += (_, _) =>
                        {
                            _controller.Sample = series;
                        };

                        series.AddSamples(yData, false);

                        for (
                            int i = 0;
                            i < _controller.Charts[0].ViewXY.LineSeriesCursors.Count;
                            i++
                        )
                        {
                            var ann = _controller.CreateAnnotation(_controller.Charts[0]);
                            _controller.Charts[0].ViewXY.Annotations.Add(ann);
                        }

                        _controller.Charts[0].ViewXY.SampleDataSeries.Add(series);
                    }

                    _controller.Charts[0].EndUpdate();

                    _controller.UpdateCursorResult(_controller.Charts[0]);
                }
            }
        }
    }
}
