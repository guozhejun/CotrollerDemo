using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CotrollerDemo.Models
{
    public class DeviceInfoModel
    {
        public IPAddress IpAddress { get; set; }

        public string SerialNum { get; set; }

        public string Status { get; set; }
    }
}
