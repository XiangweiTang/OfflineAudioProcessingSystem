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

        public static string ToStringPathShort(this DateTime dt)
        {
            return dt.ToString("yyyyMMdd");
        }

        public static string WrapPath(this string path)
        {
            if (!path.Contains(' '))
                return path;
            char c0 = path[0];
            char cn = path[path.Length - 1];
            if (c0 == '"' && cn == '"')
                return path;
            if (c0 == '\'' && cn == '\'')
                return path;
            return $"\"{path}\"";
        }
        public static string GetFirstNPart(this string s, char sep, int n = 0)
        {
            var split = s.TrimStart(sep).Split(sep);
            return split[n];
        }
        public static string GetLastNPart(this string s, char sep, int n = 1)
        {
            var split = s.TrimEnd(sep).Split(sep);
            return split[split.Length - n];
        }
    }
}
