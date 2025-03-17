using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CotrollerDemo.Models
{
    public class TcpClientModel
    {
        public TcpClient client;
        public NetworkStream stream;
        public TcpListener Tcp;

        public byte[] packLengths = [0x1D, 0, 0, 0]; // 包长度

        public byte[] hexValue = { 0xFA, 0xFB, 0xFC, 0xFD, 0xDD, 0xCC, 0xBB, 0xAA }; // 发送包头

        public byte[] typeValues = { 0x14, 1, 0x0B, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // 类型值

        public int version = 5; // 版本号

        public int packLength = 29; // 包长度

        public TcpClientModel()
        {
        }

        public void StartTcpListen(string ipAdderss)
        {
            Task.Run(async () =>
            {
                try
                {
                    Tcp = new(new IPEndPoint(IPAddress.Parse(ipAdderss), 9089));
                    Tcp.Start();

                    client = await Tcp.AcceptTcpClientAsync();

                    stream = client.GetStream();

                    await ReceiveDataClient();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
        }


        /// <summary>
        /// 接收客户端发送的数据
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public async Task ReceiveDataClient()
        {

            // 获取网络流
            stream = client.GetStream();

            byte[] buffer = new byte[1024];

            int bytesRead;
            // 读取客户端发送的数据
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                //string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                string[] hexArray = [.. buffer.Select(b => b.ToString("X2"))];
                string text = string.Join("-", hexArray);
                Debug.WriteLine(text);
            }

            // 关闭连接
            client.Close();
        }

        /// <summary>
        /// 发送数据到客户端
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task SendDataClient(int param)
        {
            if (stream != null)
            {
                byte[] data =
                 [
                     .. packLengths,
                    .. hexValue,
                    .. BitConverter.GetBytes(version),
                    .. typeValues,
                    .. BitConverter.GetBytes(packLength),
                    (byte)param
                 ];

                await stream.WriteAsync(data);
            }
        }
    }
}
