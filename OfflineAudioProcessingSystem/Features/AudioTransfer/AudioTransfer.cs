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
            var aft = new AudioFolderTransfer(Cfg.ReportPath, Cfg.ExistringFileListPath, Cfg.ErrorPath)
            {
                SampleRate = Cfg.SampleRate,
                NumChannels = Cfg.NumChannels,
                MaxParallel = 5
            };
            if (Directory.Exists(Cfg.InputPath))
            {

                aft.Run(Cfg.InputPath, Cfg.OutputPath);
            }
            else if (File.Exists(Cfg.InputPath))
            {
                aft.ConvertToWave(Cfg.InputPath, Cfg.OutputPath);
            }
            else
                throw new CommonException($"Missing input path: {Cfg.InputPath}");
        }        
    }

    class AudioFolderTransfer : FolderTransfer
    {
        const int BUFFER_SIZE = 50 * 1024;  // 50K.
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
                .ToDictionary(x => x.Key, x => x.Select(y=>y.Split('\t')[1]).ToList());
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            string ext = inputPath.Split('.').Last().ToLower();
            Sanity.Requires(ValidExtSet.Contains(ext), $"Invalid extension: {ext}");
            ConvertToWave(inputPath, outputPath);
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

        public void ConvertToWave(string inputPath, string outputPath)
        {
            LocalCommon.SetAudioWithFfmpeg(inputPath.WrapPath(), SampleRate, NumChannels, outputPath.WrapPath());
            if (!File.Exists(outputPath))
                LocalCommon.SetAudioWithSox(inputPath.WrapPath(), SampleRate, NumChannels, outputPath.WrapPath());
        }

        public bool AudioCompare(string waveFilePath1, string waveFilePath2)
        {
            Wave w1 = new Wave();
            w1.ShallowParse(waveFilePath1);
            Wave w2 = new Wave();
            w2.ShallowParse(waveFilePath1);
            using(FileStream fs1=new FileStream(waveFilePath1,FileMode.Open,FileAccess.Read))
            using(FileStream fs2=new FileStream(waveFilePath2, FileMode.Open, FileAccess.Read))
            {
                fs1.Seek(w1.DataChunk.Offset + 8, SeekOrigin.Begin);
                fs2.Seek(w2.DataChunk.Offset + 8, SeekOrigin.Begin);
                byte[] buffer1 = new byte[BUFFER_SIZE];
                byte[] buffer2 = new byte[BUFFER_SIZE];
                while (fs1.Position < fs1.Length)
                {
                    int count = Math.Min(BUFFER_SIZE, (int)(fs1.Length - fs1.Position));
                    try
                    {
                        fs1.Read(buffer1, 0, BUFFER_SIZE);
                        fs2.Read(buffer2, 0, BUFFER_SIZE);
                        if (buffer1.SequenceEqual(buffer2))
                            continue;
                        return false;
                    }
                    catch
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        public override string ItemRename(string inputItemName)
        {
            FileInfo file = new FileInfo(inputItemName);
            return file.Name.Replace(file.Extension, "wav");
        }

        protected override void PostRun()
        {
            File.WriteAllLines(ReportPath, ReportList);
            File.WriteAllLines(ErrorPath, ErrorList);
            File.AppendAllLines(ExistingFileListPath, NewFileList);
        }
    }
}
