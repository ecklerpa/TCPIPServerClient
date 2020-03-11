using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonClassLibs
{
    public class GeneralFunction
    {
        static public string GetAppPath
        {
            get
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TCPIPServerClient");
            }
        }

        static public string GetDateTimeFormatted
        {
            get
            {
                return DateTime.Now.ToShortDateString() + ", " + DateTime.Now.ToLongTimeString();
            }
        }
    }
}
