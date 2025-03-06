using CotrollerDemo.Models;
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
using System.Windows.Controls;

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

        private ObservableCollection<DeviceInfoModel> _devices;
        public ObservableCollection<DeviceInfoModel> Devices
        {
            get { return _devices; }
            set { SetProperty(ref _devices, value); }
        }

        // 存放路径
        public string folderPath = @"D:\Coding\Datas";

        public DelegateCommand DeviceSearchCommand { get; set; }

        public DelegateCommand SaveDataCommand { get; set; }

        public DelegateCommand<object> LinkCommand { get; set; }

        public ControllerViewModel()
        {
            Devices =
            [
                new(){ IpAddress="1234",Mask = "1234",Status = "未连接"},
                new(){ IpAddress="1234",Mask = "1234",Status = "未连接"},
                new(){ IpAddress="1234",Mask = "1234",Status = "未连接"},
                new(){ IpAddress="1234",Mask = "1234",Status = "未连接"},
                new(){ IpAddress="1234",Mask = "1234",Status = "未连接"},
                new(){ IpAddress="1234",Mask = "1234",Status = "未连接"}
            ];

            SaveDataCommand = new DelegateCommand(SaveData);
            LinkCommand = new DelegateCommand<object>(LinkDevice);

            GetFolderFiles();
        }

        private void LinkDevice(object obj)
        {
            var selectItem = obj as DeviceInfoModel;

            UdpClientModel.StartServer();
        }

        private void SaveData()
        {

            // 文件名
            string fileName = "Data";

            string fullPath = string.Empty;

            // 要写入文件的内容
            string content = "Hello, this is a sample text file!";

            try
            {
                // 检查文件夹是否存在，如果不存在则创建
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 创建文件并写入内容

                for (int i = 1; i <= 10; i++)
                {
                    fullPath = Path.Combine(folderPath, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + fileName + "-" + i.ToString() + ".txt"); // 组合文件夹路径和文件名
                    File.WriteAllText(fullPath, content);
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
    }

}
