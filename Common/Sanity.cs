using System;

namespace Common
{
    public static class Sanity
    {
        public static void Requires(bool valid, string message, Action exceptionPostAction=null)
        {
            if (!valid)
            {
                if (exceptionPostAction != null)
                    exceptionPostAction.Invoke();
                throw new CommonException(message);
            }
        }
        public static void Requires(bool valid, Action exceptionPostAction =null)
        {
            if (!valid)
            {
                if (exceptionPostAction != null)
                    exceptionPostAction.Invoke();
                throw new CommonException();
            }
        }
    }

    public class CommonException : Exception
    {
        public CommonException() : base() { }
        public CommonException(string message) : base(message) { }
    }
}
