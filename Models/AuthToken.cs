using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeonService.Models
{
    public class AuthToken
    {
        public string token {  get; set; }
        public string iat { get; set; }
    }
}
