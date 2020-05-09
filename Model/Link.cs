using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbtool.Model
{
    public class Link
    {
        public String Url { get; set; }
        public int Status { get; set; }
        public String Profile { get; set; }

        public Link()
        {
            Url = "";
            Status = 0;
            Profile = "";
        }

        public Link(String url, int status, string profile)
        {
            Url = url;
            Status = status;
            Profile = profile;
        }
    }
}
