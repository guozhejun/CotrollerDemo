using DryIoc.ImTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CotrollerDemo.Models
{
    public class UdpClientModel
    {
        public IPAddress serverIp; // 本机IP

        public byte[] hexValue = { 0xFA, 0xFB, 0xFC, 0xFD, 0xDD, 0xCC, 0xBB, 0xAA }; // 发送包头

        public string[] receiveValue = { "00", "00", "C0", "FF", "AA", "BB", "CC", "DD" }; // 接收包头

        public int version = 5; // 版本号

        public int packLength = 10; // 包长度

        IPEndPoint receivePoint = new(IPAddress.Parse("255.255.255.255"), 9090); // 接收客户端的IP和端口

        private UdpClient udpServer;

        public int DeviceConnectState = 0; // 设备连接状态

        public UdpClientModel(string ipAddress)
        {
            StartListen(ipAddress);
        }

        public void StartListen(string ipAddress)
        {
            try
            {
                serverIp = IPAddress.Parse(ipAddress);
                int serverPort = 8080;
                udpServer ??= new(serverPort);

                byte[] typeValues = [1, 1, 1, 0, 0, 0, 0]; // 类型值

                byte[] bufferBytes =
                [
                    .. hexValue,
                    .. BitConverter.GetBytes(version),
                    .. typeValues,
                    .. BitConverter.GetBytes(packLength),
                    .. serverIp.GetAddressBytes(),
                    .. GetMacAddress()
                ];

                udpServer.Send(bufferBytes, bufferBytes.Length, receivePoint);

                // 接收UDP服务端的响应
                byte[] receivedBytes = udpServer.Receive(ref receivePoint);

                byte[] TemporaryArray = new byte[8];

                Array.Copy(receivedBytes, TemporaryArray, 8);

                string[] hexArray = [.. TemporaryArray.Select(b => b.ToString("X2"))];

                if (receiveValue.SequenceEqual(hexArray))
                {
                    // 获取接收到的IP
                    byte[] deviceIpByte = new byte[4];
                    Array.Copy(receivedBytes, receivedBytes.Length - 23, deviceIpByte, 0, 4);

                    // 获取接收到的序列号
                    byte[] deviceSerialNumByte = new byte[16];
                    Array.Copy(receivedBytes, receivedBytes.Length - 19, deviceSerialNumByte, 0, 16);
                    string[] deviceSerialNums = [.. deviceSerialNumByte.Select(b => b.ToString("X2"))];

                    DeviceConnectState = receivedBytes[31];

                    GlobalValues.DeviceList.Clear();

                    // 将获取到的数据存到全局变量中
                    GlobalValues.DeviceList.Add(new()
                    {
                        IpEndPoint = receivePoint,
                        SerialNum = string.Join(":", deviceSerialNums),
                        Status = DeviceConnectState
                    });
                }

            }
            catch (Exception)
            {

                throw;
            }
        }


        /// <summary>
        /// 连接/断开设备
        /// </summary>
        /// <param name="iPEndPoint"></param>
        /// <param name="IsConnect">是否连接</param>
        public void IsConnectDevice(IPAddress address, bool IsConnect)
        {
            byte[] typeValues = []; // 类型值

            if (IsConnect)
            {
                typeValues = [1, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            }
            else
            {
                typeValues = [1, 1, 3, 0, 0, 0, 0];
            }

            byte[] bufferBytes =
            [
              .. hexValue,
              .. BitConverter.GetBytes(version),
              .. typeValues,
              .. BitConverter.GetBytes(packLength),
              .. serverIp.GetAddressBytes(),
              .. GetMacAddress()
            ];

            udpServer.Send(bufferBytes, bufferBytes.Length, receivePoint);

            // 接收UDP服务端的响应
            byte[] receivedBytes = udpServer.Receive(ref receivePoint);

            if (receivedBytes.Last() == 0 && DeviceConnectState != 1 && IsConnect)
            {
                GlobalValues.DeviceList.First(d => d.IpEndPoint.Address == address).Status = 1;
            }
            else if (receivedBytes.Last() == 0 && DeviceConnectState == 1 && !IsConnect)
            {
                GlobalValues.DeviceList.First(d => d.IpEndPoint.Address == address).Status = 0;
    
            }
            StartListen(address.ToString());
        }

        /// <summary>
        /// 获取本机Mac地址
        /// </summary>
        /// <returns></returns>
        public static byte[] GetMacAddress()
        {
            try
            {
                byte[] macBytes = [];

                // 获取所有网络接口
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface networkInterface in networkInterfaces)
                {
                    // 检查网络接口类型是否为以太网
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        // 获取 MAC 地址
                        macBytes = networkInterface.GetPhysicalAddress().GetAddressBytes();
                    }
                }

                return macBytes;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
