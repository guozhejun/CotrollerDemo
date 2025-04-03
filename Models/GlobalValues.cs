﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;

namespace CotrollerDemo.Models
{
    public static class GlobalValues
    {
        /// <summary>
        /// UDP客户端
        /// </summary>
        public static UdpClientModel UdpClient { get; set; } = new();

        /// <summary>
        /// TCP客户端
        /// </summary>
        public static TcpClientModel TcpClient { get; set; } = new();

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public static bool IsRunning { get; set; } = false;

        /// <summary>
        /// 全局浮点数
        /// </summary>
        public static List<List<float>> GlobalFloats { get; set; } = [];

        public static List<List<float>> GlobalFloats1 { get; set; } = [];

        public static bool IsResult { get; set; } = true;

        public static ObservableCollection<DeviceInfoModel> Devices { get; set; } = [];

        //public static ReceiveData receiveData { get; set; } = new();

        /// <summary>
        /// 获取本机IP地址
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetIpAdderss()
        {
            string hostName = Dns.GetHostName();

            // 获取主机名对应的所有 IP 地址
            IPAddress[] addresses = Dns.GetHostEntry(hostName).AddressList;

            // 过滤出 IPv4 地址
            IPAddress ipAddress = addresses.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (ipAddress != null)
            {
                return ipAddress;
            }
            else
            {
                return IPAddress.Loopback;
            }
        }
    }
}