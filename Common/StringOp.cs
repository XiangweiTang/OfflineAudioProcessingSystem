using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class StringOp
    {
        public static string ToStringLog(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd hh:mm:ss.fff");
        }
    }
}
