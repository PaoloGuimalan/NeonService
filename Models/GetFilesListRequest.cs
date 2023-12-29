using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeonService.Models
{
    public class GetFilesListRequest
    {
        public string deviceID { get; set; }
        public string path { get; set; }
    }
}
