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
    }

    class ATransfer : FolderTransfer
    {
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            LocalCommon.SetAudioToWaveWithFfmpeg(inputPath, outputPath);
        }
    }
}
