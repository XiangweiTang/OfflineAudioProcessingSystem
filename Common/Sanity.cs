using System;

namespace Common
{
    public static class Sanity
    {
        public static void Requires(bool valid, string message)
        {
            if (!valid)
                throw new CommonException(message);
        }
        public static void Requires(bool valid)
        {
            if (!valid)
                throw new CommonException();
        }
    }

    public class CommonException : Exception
    {
        public CommonException() : base() { }
        public CommonException(string message) : base(message) { }
    }
}
