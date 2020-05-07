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

        public Profile()
        {
            Path = "";
            Facebook = "";
        }

        public Profile(String path, String facebook)
        {
            Path = path;
            Facebook = facebook;
        }
    }
}
