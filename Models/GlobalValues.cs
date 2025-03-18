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

        public static UdpClientModel UdpClient { get; set; } = new();

        public static TcpClientModel TcpClient { get; set; } = new();

        public static bool IsRunning { get; set; } = false;

        public static List<List<float>> GlobalFloats { get; set; } = [];

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
