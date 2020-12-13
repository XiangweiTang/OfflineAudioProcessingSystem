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
                var aft = new AudioFolderTransfer(Cfg.ReportPath, Cfg.ExistringFileListPath, Cfg.ErrorPath)
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
        private List<string> ErrorList = new List<string>();
        private string ReportPath = "";
        private string ErrorPath = "";
        private Dictionary<string, List<string>> ExistingFileDict = new Dictionary<string, List<string>>();
        private string ExistingFileListPath = "";
        private List<string> NewFileList = new List<string>();
        public AudioFolderTransfer(string reportPath, string existingFileListPath, string errorPath) : base()
        {
            ReportPath = reportPath;
            ExistingFileListPath = existingFileListPath;
            ErrorPath = errorPath;
        }
        protected override void PreRun()
        {
            ReportList.Add("Original name\tWave name\tDuration(s)");
            ValidExtSet = IO.ReadEmbed($"{LocalConstants.LOCAL_ASMB_NAME}.Internal.Data.AudioInputExt.txt", LocalConstants.LOCAL_ASMB_NAME)
                .ToHashSet();
            ExistingFileDict = File.ReadLines(ExistingFileListPath)
                .GroupBy(x => x.Split('\t')[0])
                .ToDictionary(x => x.Key, x => x.ToList());
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            string ext = inputPath.Split('.').Last().ToLower();
            Sanity.Requires(ValidExtSet.Contains(ext), $"Invalid extension: {ext}");
            LocalCommon.SetAudio(inputPath.WrapPath(), SampleRate, NumChannels, outputPath.WrapPath());
            Wave w = new Wave();
            w.ShallowParse(outputPath);
            string reportLine = $"{inputPath.Split('\\').Last()}\t{outputPath.Split('\\').Last()}\t{w.AudioLength}";
            string key = w.DataChunk.Length.ToString();
            lock (LockObj)
            {
                if (ExistingFileDict.ContainsKey(key))
                {
                    foreach (string existingFilePath in ExistingFileDict[key])
                    {
                        if (AudioCompare(existingFilePath, outputPath))
                        {
                            ErrorList.Add($"{outputPath}\t{existingFilePath}");
                            return;
                        }
                    }
                    ExistingFileDict[key].Add(outputPath);
                }
                else
                    ExistingFileDict[key] = new List<string> { outputPath };
                ReportList.Add(reportLine);
                NewFileList.Add($"{key}\t{outputPath}");
            }
        }

        private bool AudioCompare(string waveFile1, string waveFile2)
        {
            Wave w1 = new Wave();
            w1.ShallowParse(waveFile1);            
            Wave w2 = new Wave();
            w2.ShallowParse(waveFile2);

            if (w1.DataChunk.Length != w2.DataChunk.Length)
                return false;
            using(FileStream fs1=new FileStream(waveFile1,FileMode.Open,FileAccess.Read))
            using(FileStream fs2=new FileStream(waveFile2, FileMode.Open, FileAccess.Read))
            {
                fs1.Seek(w1.DataChunk.Offset + 8, SeekOrigin.Begin);
                fs2.Seek(w2.DataChunk.Offset + 8, SeekOrigin.Begin);
                while (fs1.Position < fs1.Length)
                {
                    try
                    {
                        if (fs1.ReadByte() == fs2.ReadByte())
                            continue;
                        else
                            return false;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return true;
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
            File.WriteAllLines(ErrorPath, ErrorList);
            File.AppendAllLines(ExistingFileListPath, NewFileList);
        }
    }
}
