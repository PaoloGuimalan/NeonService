using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeonService.Models
{
    public class DataJson
    {
        public bool status { get; set; }

        public string listener { get; set; }

        public string result { get; set; }
    }
    public class ResponseData
    {
        public DataJson data { get; set; }
    }
}
