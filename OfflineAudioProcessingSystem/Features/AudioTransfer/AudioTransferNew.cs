using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;

namespace OfflineAudioProcessingSystem.AudioTransfer
{
    class AudioTransferNew
    {
        
    }
    class AudioCheckTransfer : FolderTransfer
    {
        public List<string> ErrorList { get; set; } = new List<string>();
        private int SampleRate { get; set; } = 16000;
        private int BitsPerSample { get; set; } = 16;
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            LocalCommon.SetAudioToWaveWithFfmpeg(inputPath, outputPath);
            Wave w = new Wave();
            w.ShallowParse(outputPath);
            if (w.SampleRate <= SampleRate)
            {
                ErrorList.Add($"{inputPath}\t{w.SampleRate}");
                
            }
        }

        public override string ItemRename(string inputItemName)
        {
            string ext = inputItemName.Split('.').Last();
            return inputItemName.Replace("." + ext, ".wav");
        }
    }
}
