using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Sequence
    {
        public static Random R = new Random();
        public static IEnumerable<T> ToSequence<T>(this T t)
        {
            return new T[] { t };
        }
        public static void GoThrough<T> (this IEnumerable<T> seq)
        {
            foreach (T _ in seq) ;
        }
        public static IEnumerable<T> Concat<T>(this T t, IEnumerable<T> tSequence)
        {
            return t.ToSequence().Concat(tSequence);
        }

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> tSequence, T t)
        {
            return tSequence.Concat(t.ToSequence());
        }

        public static T[] Shuffle<T>(this IEnumerable<T> tSequence)
        {
            var array = tSequence.ToArray();
            for(int i = array.Length - 1; i > 0; i--)
            {
                int j = R.Next(i);
                T t = array[i];
                array[i] = array[j];
                array[j] = t;
            }
            return array;
        }

        public static T ArgMax<T>(Func<T,T,bool> maxComparer, params T[] seq)
        {
            return seq.ArgMax(maxComparer);
        }

        public static T ArgMax<T>(this IEnumerable<T> seq, Func<T, T, bool> maxComparer)
        {
            return seq.Aggregate((x, y) => maxComparer(x, y) ? x : y);
        }
    }
}
