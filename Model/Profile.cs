using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbtool.Model
{
    public class Profile
    {
        public string Fid { get; set; }
        public string Password { get; set; }
        public string SecretKey { get; set; }

        public Profile()
        {
            Fid = "";
            Password = "";
            SecretKey = "";
        }

        public Profile(string fid, string password, string secretKey)
        {
            Fid = fid;
            Password = password;
            SecretKey = secretKey;
        }
    }
}
