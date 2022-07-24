using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdgeGatewayApp.Model
{
    public class LogData
    {
        public string TagName { get; set; }
        public DateTime DateTime { get; set; }
        public string Value { get; set; }
        public string ModbusAddress { get; set; }
    }

    public class InsightLogData
    {
        public string FileName { get; set; }
        public string Status { get; set; }
        public string UploadedDateTime { get; set; }
    }

    public class MqttData
    {
        public string TagName { get; set; }
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
        public string ModbusAddress { get; set; }
    }
}
