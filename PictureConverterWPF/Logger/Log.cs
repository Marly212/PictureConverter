using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureConverterWPF.Logger
{
    class Log
    {
        public static void Ging(string me)
        {
            File.AppendAllLines("log", new string[] { me });
        }
    }
}
