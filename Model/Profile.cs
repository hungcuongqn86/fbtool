using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbtool.Model
{
    public class Profile
    {
        public String Path { get; set; }
        public String Facebook { get; set; }
        public String UserName { get; set; }
        public String Password { get; set; }

        public Profile()
        {
            Path = "";
            Facebook = "";
            UserName = "";
            Password = "";
        }

        public Profile(String path, String facebook, String userName, String password)
        {
            Path = path;
            Facebook = facebook;
            UserName = userName;
            Password = password;
        }
    }
}
