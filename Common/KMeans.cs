using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class KMeans<T>
    {
        private static Func<T, T, double> CalcDist = null;
        private static ValueTypePair<T>[] LocalArray = null;
        private static T[] ClusterArray = null;
        private static List<T>[] ClusterSetArray = null;
        private static Func<IEnumerable<T>, T> CalcCentroid = null;
        static KMeans()
        {

        }

        public static void Run(Func<T,T,double> calcDist, Func<IEnumerable<T>, T> calcCentroid, ValueTypePair<T>[] sequence, T[] clusterArray, int n)
        {
            CalcDist = calcDist;
            CalcCentroid = calcCentroid;
            LocalArray = sequence;
            ClusterArray = clusterArray;
            ClusterSetArray = new List<T>[ClusterArray.Length];

            for(int i = 0; i < n; i++)
            {
                Init();
                Iteration();
                Update();
            }            
        }

        private static void Init()
        {
            for (int i = 0; i < ClusterSetArray.Length; i++)
            {
                ClusterSetArray[i] = new List<T>();
            }
        }
        private static void Iteration()
        {
            for(int i = 0; i < LocalArray.Length; i++)
            {
                double min = double.MaxValue;
                for(int j = 0; j < ClusterArray.Length; j++)
                {
                    double dist = CalcDist(LocalArray[i].Value, ClusterArray[j]);
                    if (dist < min)
                    {
                        min = dist;
                        LocalArray[i].Type = j;                        
                    }
                    ClusterSetArray[LocalArray[i].Type].Add(LocalArray[i].Value);
                }
            }            
        }
        private static void Update()
        {
            for(int i = 0; i < ClusterSetArray.Length; i++)
            {
                ClusterArray[i] = CalcCentroid(ClusterSetArray[i]);
            }
        }
    }

    public struct ValueTypePair<T>
    {
        public T Value { get; set; }
        public int Type { get; set; }
    }

    
}
