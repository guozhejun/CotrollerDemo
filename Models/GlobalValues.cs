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

    }
}
