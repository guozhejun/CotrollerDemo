using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CotrollerDemo.Models
{
    public static class GlobalValues
    {
        public static string Name { get; set; }

        public static UdpClientModel UdpClient { get; set; }
        public static TcpClientModel TcpClient { get; set; }

        public static ObservableCollection<Devices> DeviceList { get; set; } = [];
    }

    public class Devices
    {
        /// <summary>
        /// Ip地址
        /// </summary>
        public IPEndPoint IpEndPoint { get; set; }
        /// <summary>
        /// 设备序列号
        /// </summary>
        public string SerialNum { get; set; }
        /// <summary>
        /// 是否连接
        /// </summary>
        public int Status { get; set; }
    }
}
