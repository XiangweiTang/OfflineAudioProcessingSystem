using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Common
{
    public static class IO
    {
        public static string ReadStringFromFileStream(this FileStream fs, Encoding encoding, int count)
        {
            byte[] buffer = new byte[count];
            fs.Read(buffer, 0, count);
            return encoding.GetString(buffer);
        }

        public static int ReadIntFromFileStream(this FileStream fs)
        {
            byte[] buffer = new byte[4];
            fs.Read(buffer, 0, 4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static short ReatShortFromFileStream(this FileStream fs)
        {
            byte[] buffer = new byte[2];
            fs.Read(buffer, 0, 2);
            return BitConverter.ToInt16(buffer, 0);
        }
    }
}
