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

        public override string GetFeatureName()
        {
            return "AudioTransfer";
        }

        protected override void LoadConfig(string configPath)
        {
            Cfg.Load(configPath);
        }

        protected override void Run()
        {
            if (Directory.Exists(Cfg.InputPath))
            {
                var aft = new AudioFolderTransfer(Cfg.ReportPath)
                {
                    SampleRate = Cfg.SampleRate,
                    NumChannels = Cfg.NumChannels,
                    MaxParallel = 5
                };

                aft.Run(Cfg.InputPath, Cfg.OutputPath);
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
        private List<string> ReportList = new List<string>();
        private string ReportPath = "";
        public AudioFolderTransfer(string reportPath) : base()
        {
            ReportPath = reportPath;
        }
        protected override void PreRun()
        {
            ReportList.Add("Original name\tWave name\tDuration(s)");
            ValidExtSet = IO.ReadEmbed($"{LocalConstants.LOCAL_ASMB_NAME}.Internal.Data.AudioInputExt.txt", LocalConstants.LOCAL_ASMB_NAME)
                .ToHashSet();
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            string ext = inputPath.Split('.').Last().ToLower();
            Sanity.Requires(ValidExtSet.Contains(ext), $"Invalid extension: {ext}");
            LocalCommon.SetAudio(inputPath.WrapPath(), SampleRate, NumChannels, outputPath.WrapPath());
            Wave w = new Wave();
            w.ShallowParse(outputPath);
            string reportLine = $"{inputPath.Split('\\').Last()}\t{outputPath.Split('\\').Last()}\t{w.AudioLength}";
            lock (LockObj)
            {
                ReportList.Add(reportLine);
            }
        }

        public override string ItemRename(string inputItemName)
        {
            Sanity.Requires(inputItemName.Contains('.'), $"No extension: {inputItemName}");
            int i = inputItemName.LastIndexOf('.');
            string prefix = inputItemName.Substring(0, i);
            return $"{prefix}.wav";
        }

        protected override void PostRun()
        {
            File.WriteAllLines(ReportPath, ReportList);
        }
    }
}
