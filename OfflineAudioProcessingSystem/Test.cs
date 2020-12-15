using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;

namespace OfflineAudioProcessingSystem
{
    class Test
    {
        public Test(string[] args)
        {
            string w1 = @"D:\WorkFolder\Input\300hrsRecordingContent\Zurich\Zürich 14.12.2020\Zürich+38+FEMALE+27.wav";
            string w2 = @"D:\WorkFolder\Input\300hrsRecordingContent\Zurich\Zürich 13.12.2020 F\Zürich+16+FEMALE+27.wav";
            var b=CompareWaves(w1, w2);
        }

        private void GenerateRecords()
        {
            string Path = @"D:\WorkFolder\Input\300hrsRecordingContent";
            string o = @"D:\WorkFolder\Input\Summary.txt";
            Dictionary<int, List<string>> dict = new Dictionary<int, List<string>>();
            foreach(string wavPath in Directory.EnumerateFiles(Path, "*.wav", SearchOption.AllDirectories))
            {
                Wave w = new Wave();
                w.ShallowParse(wavPath);
                if (!dict.ContainsKey(w.DataChunk.Length))
                    dict[w.DataChunk.Length] = new List<string> { wavPath };
                else
                    dict[w.DataChunk.Length].Add(wavPath);
            }
            var list = dict.SelectMany(x => x.Value.Select(y => $"{x.Key}\t{y}"));
            File.WriteAllLines(o, list);
        }
        private void Browse()
        {
            string path = @"D:\WorkFolder\Input\Summary.txt";
            var groups = File.ReadLines(path)
                .GroupBy(x => x.Split('\t')[0]);
            foreach(var group in groups)
            {
                var array = group.Select(x=>x.Split('\t')[1]).ToArray();
                if (array.Length <= 1)
                    continue;
                Console.WriteLine(group.Key);
                for(int i = 0; i < array.Length-1; i++)
                {
                    for(int j = i + 1; j < array.Length; j++)
                    {
                        if (CompareWaves(array[i], array[j]))
                            Console.WriteLine($"\t{i}\t{j}");
                    }
                }
            }
        }

        
        private bool CompareWaves(string wave1, string wave2)
        {
            Wave w1 = new Wave();
            Wave w2 = new Wave();
            w1.ShallowParse(wave1);
            w2.ShallowParse(wave2);

            using(FileStream fs1=new FileStream(wave1,FileMode.Open,FileAccess.Read))
            using(FileStream fs2=new FileStream(wave2, FileMode.Open, FileAccess.Read))
            {
                fs1.Seek(w1.DataChunk.Offset + 8, SeekOrigin.Begin);
                fs2.Seek(w2.DataChunk.Offset + 8, SeekOrigin.Begin);
                try
                {
                    while (fs1.Position < fs1.Length)
                    {
                        if (fs1.ReadByte() == fs2.ReadByte())
                            continue;
                        Console.WriteLine(fs1.Position);
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
    }
}
