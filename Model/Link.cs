using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbtool.Model
{
    public class Link
    {
        public string Key { get; set; }
        public string Url { get; set; }
        public int Status { get; set; }
        public string Profile { get; set; }

        public Link()
        {
            Key = "";
            Url = "";
            Status = 0;
            Profile = "";
        }

        public Link(string url, int status, string profile, string key)
        {
            Key = key;
            Url = url;
            Status = status;
            Profile = profile;
        }
    }
}
