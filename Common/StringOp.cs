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

        public static string ToStringPathLong(this DateTime dt)
        {
            return dt.ToString("yyyyMMdd_hhmmss");
        }

        public static string WrapPath(this string path)
        {
            char c0 = path[0];
            char cn = path[path.Length - 1];
            if (c0 == '"' && cn == '"')
                return path;
            if (c0 == '\'' && cn == '\'')
                return path;
            return $"\"{path}\"";
        }
    }
}
