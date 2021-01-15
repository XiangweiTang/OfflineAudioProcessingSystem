using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OfflineAudioProcessingSystem
{
    class Test
    {
        Random R = new Random();
        public Test(string[] args)
        {
        }

        private void CreatePairfiles(string rootPath, string fileName, int maxAudioLength)
        {
            string wavePath = Path.Combine(rootPath, fileName + ".wav");
            string textPath = Path.Combine(rootPath, fileName + ".txt");
            Wave.CreateDummyPCMWave(wavePath, maxAudioLength, 1, 16000, 16);
            var list = GenerateDummyTimeStamps(maxAudioLength);
            File.WriteAllLines(textPath, list);
        }

        private IEnumerable<string> GenerateDummyTimeStamps(int maxAudioLength)
        {
            const double SAFETY_BUFF = 0.5;
            int baseDurationStep = maxAudioLength / 50;
            int basePauseStep = maxAudioLength / 100;
            double lastEnd = 0;
            Random R = new Random();            
            while (true)
            {
                double currentStart = lastEnd + basePauseStep * (1 + R.NextDouble());
                double currentEnd = currentStart + baseDurationStep * (1 + R.NextDouble());
                lastEnd = currentEnd;
                if (currentEnd < maxAudioLength - SAFETY_BUFF)
                    yield return $"{currentStart}\t{currentEnd}";
                else
                    break;
            }
        }

        private double CalcDist((double,int) d1, (double,int) d2)
        {
            return (d1.Item1 - d2.Item1) * (d1.Item1 - d2.Item1);
        }
        private (double,int) CalcCentroid(IEnumerable<(double,int)> sequence)
        {
            return (sequence.Average(x => x.Item1), -1);
        }
    }
}
