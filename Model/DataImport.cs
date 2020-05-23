using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbtool.Model
{
    public class DataImport
    {
        public String Data { get; set; }

        public DataImport()
        {
            Data = "";
        }

        public DataImport(String data)
        {
            Data = data;
        }
    }
}
