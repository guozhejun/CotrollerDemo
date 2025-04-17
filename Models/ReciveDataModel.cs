using SqlSugar;
using System.Collections.Generic;

namespace CotrollerDemo.Models
{
    public class ReceiveData
    {
        public int Segments { get; set; }
        public int ChannelId { get; set; }
        public float[] Data { get; set; } = [];
    }

    public class ReceiveSql
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public string FileName { get; set; }
        public int ChannelId { get; set; }
        public string Data { get; set; }
    }
}