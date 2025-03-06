using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CotrollerDemo.Models
{
    public static class UdpClientModel
    {
        private const int ListenPort = 3234;

        private static string IpAdd;

        private static readonly UdpClient udpServer = new(ListenPort);
        public static void StartServer()
        {
            StartListener();
        }
        private static void StartListener()
        {
            udpServer.BeginReceive(ReceiveCallback, null);
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(IpAdd), ListenPort);
        }
    }
}
