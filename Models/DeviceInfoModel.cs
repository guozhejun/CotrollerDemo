﻿using System.Net;

namespace CotrollerDemo.Models
{
    /// <summary>
    /// 设备信息模型
    /// </summary>
    public class DeviceInfoModel
    {
        /// <summary>
        /// Ip地址
        /// </summary>
        public IPAddress IpAddress { get; set; }

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string SerialNum { get; set; }

        /// <summary>
        /// 连接状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 连接设备IP
        /// </summary>
        public IPAddress LinkIp { get; set; }
    }
}