using CotrollerDemo.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CotrollerDemo.ViewModels
{
    public class ControllerViewModel : BindableBase
    {

        private ObservableCollection<string> _folderName;

        public ObservableCollection<string> FolderName
        {
            get { return _folderName; }
            set { SetProperty(ref _folderName, value); }
        }

        private ObservableCollection<DeviceInfoModel> _devices;
        public ObservableCollection<DeviceInfoModel> Devices
        {
            get { return _devices; }
            set { SetProperty(ref _devices, value); }
        }

        public DelegateCommand 


        public ControllerViewModel()
        {
            Devices =
            [
                new(){ IpAddress="1234",Mask = "1234",Status = true},
                new(){ IpAddress="1234",Mask = "1234",Status = true},
                new(){ IpAddress="1234",Mask = "1234",Status = true},
                new(){ IpAddress="1234",Mask = "1234",Status = true},
                new(){ IpAddress="1234",Mask = "1234",Status = true},
                new(){ IpAddress="1234",Mask = "1234",Status = true}
            ];
        }
    }

}
