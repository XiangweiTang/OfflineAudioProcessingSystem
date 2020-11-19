using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;

namespace OfflineAudioProcessingSystem.AudioTransfer
{
    class AudioTransfer : Feature
    {
        ConfigAudioTransfer Cfg = new ConfigAudioTransfer();
        protected override void LoadConfig(string configPath)
        {
            Cfg.Load(configPath);
        }

        protected override void Run()
        {
            if (Directory.Exists(Cfg.InputPath))
            {
                var aft = new AudioFolderTransfer()
                {
                    SampleRate = Cfg.SampleRate,
                    NumChannels = Cfg.NumChannels,
                    MaxParallel = 5
                };

                new AudioFolderTransfer().Run(Cfg.InputPath, Cfg.OutputPath);
            }
            else if (File.Exists(Cfg.InputPath))
            {
                LocalCommon.SetAudio(Cfg.InputPath, Cfg.SampleRate, Cfg.NumChannels, Cfg.OutputPath);
            }
            else
                throw new CommonException($"Missing input path: {Cfg.InputPath}");
        }

        
    }

    class AudioFolderTransfer : FolderTransfer
    {
        HashSet<string> ValidExtSet = new HashSet<string>();
        public int SampleRate { get; set; } = 16000;
        public int NumChannels { get; set; } = 1;
        protected override void PreRun()
        {
            ValidExtSet = IO.ReadEmbed($"{LocalConstants.LOCAL_ASMB_NAME}.Internal.Data.AudioInputExt.txt", LocalConstants.LOCAL_ASMB_NAME)
                .ToHashSet();
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            string ext = inputPath.Split('.').Last().ToLower();
            Sanity.Requires(ValidExtSet.Contains(ext), $"Invalid extension: {ext}");
            inputPath = inputPath.WrapPath();
            outputPath = outputPath.WrapPath();
            LocalCommon.SetAudio(inputPath, SampleRate, NumChannels, outputPath);
        }

        public override string ItemRename(string inputItemName)
        {
            Sanity.Requires(inputItemName.Contains('.'), $"No extension: {inputItemName}");
            int i = inputItemName.LastIndexOf('.');
            string prefix = inputItemName.Substring(0, i);
            return $"{prefix}.wav";
        }
    }
}
