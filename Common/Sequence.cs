using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Sequence
    {
        public static IEnumerable<T> ToSequence<T>(this T t)
        {
            return new T[] { t };
        }

        public static IEnumerable<T> Concat<T>(this T t, IEnumerable<T> tSequence)
        {
            return t.ToSequence().Concat(tSequence);
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> tSequence, T t)
        {
            return tSequence.Concat(t.ToSequence());
        }
    }
}
