using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fbtool.Model
{
    public class DataImport
    {
        public int Count { get; set; }
        public string Data { get; set; }

        public DataImport()
        {
            Data = "";
            Count = 1;
        }

        public DataImport(string data, int count)
        {
            Count = count;
            Data = data;
        }
    }
}
