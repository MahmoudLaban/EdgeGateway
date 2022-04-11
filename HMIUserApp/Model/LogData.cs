using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMIUserApp.Model
{
    public class LogData
    {
        public string TagName { get; set; }
        public DateTime DateTime { get; set; }
        public string Value { get; set; }
        public string ModbusAddress { get; set; }
    }
}
