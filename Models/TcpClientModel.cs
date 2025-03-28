using DevExpress.Internal.WinApi.Windows.UI.Notifications;
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
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shell;
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

        public float[][] sineWaveData = new float[8][]; // 正弦波数据

        public byte[] byteArray = new byte[1024]; // 字节数组

        public float[] floatArray = new float[256]; // 浮点数数组

        public int ChannelID; // 通道ID

        public int Segments; // 分段数

        private Channel<ReceiveData> _dataChannel;
        public ChannelWriter<ReceiveData> ChannelWriter { get; set; }
        public ChannelReader<ReceiveData> ChannelReader { get; set; }

        //private Channel<List<ReceiveData>> _dataChannel;
        //public ChannelWriter<List<ReceiveData>> ChannelWriter { get; set; }
        //public ChannelReader<List<ReceiveData>> ChannelReader { get; set; }

        public List<List<float>> SineWaveList { get; set; } = []; // 正弦波数据

        public List<ReceiveData> receiveDatas = [];

        public int Num = 0;
        //public ConcurrentQueue<ReceiveData[]> _dataQueue { get; set; } = []; // 数据队列

        private CancellationTokenSource _cts;

        public TcpClientModel()
        {
            _cts = new CancellationTokenSource();
            _dataChannel = Channel.CreateUnbounded<ReceiveData>(new UnboundedChannelOptions()
            {
                AllowSynchronousContinuations = true,
                SingleReader = false,
                SingleWriter = true,
            });
            //_dataChannel = Channel.CreateUnbounded<List<ReceiveData>>();
            ChannelWriter = _dataChannel.Writer;
            ChannelReader = _dataChannel.Reader;

            if (SineWaveList.Count <= 0)
            {
                for (int i = 0; i < 8; i++)
                {
                    SineWaveList.Add([]);
                }
            }


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

                    client = await Tcp.AcceptTcpClientAsync().ConfigureAwait(false);

                    client.ReceiveBufferSize = 1072;
                    client.ReceiveTimeout = 3000;

                    stream = client.GetStream();

                    ReceiveDataClient();

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            });
        }

        int tempId = -1;
        /// <summary>
        /// 接收客户端发送的数据
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public void ReceiveDataClient()
        {
            Task.Run(async () =>
            {
                byte[] Buffers = new byte[1072];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(Buffers)) != 0 && !_cts.IsCancellationRequested)
                {
                    // 将数据放入队列
                    byte[] data = new byte[bytesRead];
                    Buffer.BlockCopy(Buffers, 0, data, 0, bytesRead);

                    // 处理数据

                    ChannelID = Buffers[40];

                    Segments = Buffers[34];

                    floatArray = ConvertByteToFloat([.. Buffers.Skip(48)]);

                    ReceiveData receiveData = new()
                    {
                        ChannelID = ChannelID,
                        Segments = Segments,
                        Data = floatArray
                    };

                    if (tempId != ChannelID && receiveData.Data.Length == 256)
                    {
                        await ChannelWriter.WriteAsync(receiveData);
                        tempId = ChannelID;
                    }

                    //if (tempId != ChannelID)
                    //{
                    //    receiveDatas.Add(receiveData);
                    //    tempId = ChannelID;

                    //    if (ChannelID == 7 && Segments == 1)
                    //    {
                    //        receiveNum++;
                    //        tempReceive.AddRange(receiveDatas);

                    //        if (receiveNum >= 2)
                    //        {
                    //            Debug.WriteLine("tempReceive.Count：" + tempReceive.Count);
                    //            await ChannelWriter.WriteAsync(tempReceive);
                    //            tempReceive.Clear();
                    //            receiveNum = 0;
                    //        }
                    //    }
                    //}

                }
            });
        }

        /// <summary>
        /// 将字节数组转换为浮点数数组
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        private float[] ConvertByteToFloat(byte[] byteArray)
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
