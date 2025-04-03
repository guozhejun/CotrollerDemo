namespace CotrollerDemo.Models
{
    public class ReceiveData
    {
        public int Segments { get; set; }
        public int ChannelID { get; set; }
        public float[] Data { get; set; } = [];
    }
}