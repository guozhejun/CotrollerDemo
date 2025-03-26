using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CotrollerDemo.Models
{
    public class ReceiveData
    {
        public int Segments { get; set; }
        public int ChannelID { get; set; }
        public float[] Data { get; set; } = [];
    }
}
