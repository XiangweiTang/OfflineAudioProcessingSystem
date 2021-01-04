using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;
using System.IO.Compression;

namespace OfflineAudioProcessingSystem
{
    class Test
    {
        public Test(string[] args)
        {
            string path = @"D:\WorkFolder\Transcripts\20201224\Zip";
            List<string> list = new List<string>();
            foreach(string zipPath in Directory.EnumerateFiles(path, "*.zip"))
            {
                try
                {
                    string id = zipPath.Split('\\').Last().Split('_')[0];
                    using (FileStream fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
                    {
                        ZipArchive zip = new ZipArchive(fs);
                        var firstEntry = zip.Entries.Select(x => x.FullName).First();
                        list.Add($"{id}\t{firstEntry}");
                    }
                }
                catch
                {
                    list.Add(zipPath);
                }
            }
            File.WriteAllLines(@"D:\WorkFolder\Transcripts\20201224\id_mapping.txt", list);
        }
    }
}
