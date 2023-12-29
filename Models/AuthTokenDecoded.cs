using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeonService.Models
{
    public class AuthTokenDecoded
    {
        public string email { get; set; }
        public string userID { get; set; }
        public string connectionType { get; set; }
        public string deviceID { get; set; }
        public string iat { get; set; }
    }
}
