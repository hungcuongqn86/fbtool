using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbtool.Model
{
    public class Via
    {
        public String Data { get; set; }

        public Via()
        {
            Data = "";
        }

        public Via(String data)
        {
            Data = data;
        }
    }
}
