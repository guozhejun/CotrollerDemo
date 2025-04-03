using DevExpress.Mvvm.Native;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CotrollerDemo.Models
{
    public class UdpClientModel
    {
        public IPAddress serverIp; // 本机IP

        public byte[] hexValue = [0xFA, 0xFB, 0xFC, 0xFD, 0xDD, 0xCC, 0xBB, 0xAA]; // 发送包头

        public string[] receiveValue = ["00", "00", "C0", "FF", "AA", "BB", "CC", "DD"]; // 接收包头

        public int version = 5; // 版本号

        public int packLength = 10; // 包长度

        private IPEndPoint receivePoint = new(IPAddress.Parse("255.255.255.255"), 9090); // 接收客户端的IP和端口

        public UdpClient udpServer; // UDP服务端

        public int DeviceConnectState = 0; // 设备连接状态

        public UdpClientModel()
        {
            serverIp = GlobalValues.GetIpAdderss();
            int serverPort = 8080;
            udpServer = new(serverPort);
            ReceiveData();
        }

        /// <summary>
        /// 开始监听UDP接口
        /// </summary>
        /// <returns></returns>
        public void StartUdpListen()
        {
            GlobalValues.Devices = [];
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

            udpServer.SendAsync(bufferBytes, bufferBytes.Length, new(IPAddress.Parse("255.255.255.255"), 9090));
        }

        public void ReceiveData()
        {
            try
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        var result = udpServer.Receive(ref receivePoint);

                        if (result.Length > 0)
                        {
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                ProcessDataAsync(result);
                            });
                        }
                        await Task.Delay(10);
                    }
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void ProcessDataAsync(byte[] data)
        {
            byte[] TemporaryArray = new byte[8];

            Array.Copy(data, TemporaryArray, 8);

            string[] hexArray = [.. TemporaryArray.Select(b => b.ToString("X2"))];

            if (receiveValue.SequenceEqual(hexArray) && data.Length > 29)
            {
                // 获取接收到的IP
                byte[] deviceIpByte = new byte[4];
                Array.Copy(data, data.Length - 23, deviceIpByte, 0, 4);

                IPAddress linkIp = new(deviceIpByte);

                // 获取接收到的序列号
                byte[] deviceSerialNumByte = new byte[16];
                Array.Copy(data, data.Length - 19, deviceSerialNumByte, 0, 16);
                string[] deviceSerialNums = [.. deviceSerialNumByte.Select(b => b.ToString("X2"))];

                DeviceConnectState = data[31];

                GlobalValues.Devices.Add(new DeviceInfoModel()
                {
                    IpAddress = receivePoint.Address,
                    SerialNum = string.Join(":", deviceSerialNums),
                    Status = DeviceConnectState is 1 ? "已连接" : "未连接",
                    LinkIP = linkIp
                });
            }
        }

        /// <summary>
        /// 连接/断开设备
        /// </summary>
        /// <param name="iPEndPoint"></param>
        /// <param name="IsConnect">是否连接</param>
        public void IsConnectDevice(IPAddress ip, bool IsConnect)
        {
            byte[] typeValues; // 类型值

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

            udpServer.Send(bufferBytes, bufferBytes.Length, new IPEndPoint(ip, 9090));
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

        public void StopUdpListen()
        {
            try
            {
                udpServer?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error stopping UDP listener: " + ex.Message);
            }
        }
    }
}