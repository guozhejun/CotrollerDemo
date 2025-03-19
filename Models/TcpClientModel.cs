using DevExpress.Mvvm.Native;
using DryIoc.ImTools;
using Microsoft.VisualBasic;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static DevExpress.Utils.HashCodeHelper.Primitives;

namespace CotrollerDemo.Models
{
    public class TcpClientModel
    {
        public TcpClient client;
        public NetworkStream stream;
        public TcpListener Tcp;

        public byte[] packLengths = [0x1D, 0, 0, 0]; // 包长度

        public byte[] hexValue = { 0xFA, 0xFB, 0xFC, 0xFD, 0xDD, 0xCC, 0xBB, 0xAA }; // 发送包头

        public string[] receiveValue = { "C0", "FF", "AA", "BB", "CC", "DD" }; // 接收包头

        public byte[] typeValues = { 0x14, 1, 0x0B, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // 类型值

        public int version = 5; // 版本号

        public int packLength = 29; // 包长度

        public string[] packHead = new string[48]; // 包头

        public float[][] sineWaveData = new float[8][]; // 正弦波数据

        public byte[] byteArray = new byte[1024]; // 字节数组

        public float[] floatArray = new float[256]; // 浮点数数组

        public int ChannelID; // 通道ID

        public List<List<float>> SineWaveList { get; set; } = []; // 正弦波数据

        private ConcurrentQueue<byte[]> _dataQueue = []; // 数据队列

        public TcpClientModel()
        {
            if (SineWaveList.Count <= 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    SineWaveList.Add([]);
                    //sineWaveData.Append([]);
                }
            }
            ProcessData();
        }

        /// <summary>
        /// 开始监听Tcp服务端
        /// </summary>
        public void StartTcpListen()
        {
            Task.Run(async () =>
            {
                try
                {
                    Tcp = new(GlobalValues.GetIpAdderss(), 9089);
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
            byte[] buffer = new byte[1072];

            int bytesRead;
            // 读取客户端发送的数据
            while ((bytesRead = await stream.ReadAsync(buffer)) != 0)
            {

                if (_dataQueue.Count > 1000)
                {
                    byte[] removeData;
                    _dataQueue.TryDequeue(out removeData);
                }
                else
                {
                    if (buffer.Length == 1072)
                    {
                        // 将数据放入队列
                        byte[] data = new byte[bytesRead];
                        Buffer.BlockCopy(buffer, 0, data, 0, bytesRead);
                        _dataQueue.Enqueue(data);
                    }
                }
            }
        }

        /// <summary>
        /// 处理数据
        /// </summary>
        private void ProcessData()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (GlobalValues.IsRunning)
                    {
                        if (_dataQueue.TryDequeue(out byte[] data) && data.Length == 1072)
                        {
                            packHead = [.. data.Take(48).Select(x => x.ToString("X2"))];

                            ChannelID = Int32.Parse(packHead[40]);

                            byteArray = [.. data.Skip(48)];

                            floatArray = ConvertByteToFloat(byteArray);

                            floatArray.ForEach(data =>
                            {
                                SineWaveList[ChannelID].Add(data);
                            });

                            if (SineWaveList[ChannelID].Count > 1024)
                            {
                                SineWaveList[ChannelID].RemoveRange(0, SineWaveList[ChannelID].Count - 1024);
                            }
                        }
                        else
                        {
                            await Task.Delay(10); // 队列为空时稍作等待
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 将字节数组转换为浮点数数组
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        private static float[] ConvertByteToFloat(byte[] byteArray)
        {
            // 每 4 个字节转换为一个 float
            int floatCount = byteArray.Length / 4;
            float[] floatArray = new float[floatCount];

            for (int i = 0; i < floatCount; i++)
            {
                // 提取 4 个字节
                byte[] bytes = new byte[4];
                Buffer.BlockCopy(byteArray, i * 4, bytes, 0, 4);

                // 将字节转换为 float
                floatArray[i] = BitConverter.ToSingle(bytes, 0);
            }

            return floatArray;
        }

        /// <summary>
        /// 发送数据到客户端
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task SendDataClient(int param)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine("ErrorMessage：" + ex.Message);
            }
        }
    }
}
