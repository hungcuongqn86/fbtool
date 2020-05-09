using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbtool.Model
{
    public class Setting
    {
        private string serverName;

        public string ServerName
        {
            get { return serverName; }
            set { serverName = value; }
        }

        private string profilePath;

        public string ProfilePath
        {
            get { return profilePath; }

            set
            {
                if (value != profilePath)
                {
                    profilePath = value;
                }
            }
        }
    }
}
