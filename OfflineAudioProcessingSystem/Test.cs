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
            Wave w = new Wave();
            string uriString = @"https://marksystemapistorage.blob.core.windows.net/chdatacollections/300hrsRecordingContent/Basel1/";
            var r = AzureUtils.ListDirectories(uriString);
        }
        private void ProcessingAudios(string folderPath, string errorPath)
        {
            Dictionary<int, List<string>> dict = new Dictionary<int, List<string>>();
            List<string> list = new List<string>();
            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                if (!filePath.ToLower().EndsWith(".wav"))
                {
                    Console.WriteLine(filePath);
                    continue;
                }
                Wave w = new Wave();
                w.ShallowParse(filePath);
                if(w.WaveType!=1)
                    Console.WriteLine(filePath);
                if(w.SampleRate!=16000)
                    Console.WriteLine(filePath);
                if(w.NumChannels!=1)
                    Console.WriteLine(filePath);
                if(!dict.ContainsKey(w.DataChunk.Length))
                {
                    dict.Add(w.DataChunk.Length, new List<string> { filePath });
                }
                else
                {
                    foreach(string existingPath in dict[w.DataChunk.Length])
                    {
                        if (LocalCommon.AudioIdentical(existingPath, filePath))
                        {
                            list.Add($"{existingPath}\t{filePath}");
                            File.Delete(filePath);
                            break;
                        }
                    }
                    if (File.Exists(filePath))
                        dict[w.DataChunk.Length].Add(filePath);
                }
            }
            File.WriteAllLines(errorPath, list);
        }

    }
}
