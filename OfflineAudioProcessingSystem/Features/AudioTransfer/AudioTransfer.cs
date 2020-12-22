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
            string waveWorkFolder = Path.Combine(WorkFolder, "Wave");
            Directory.CreateDirectory(waveWorkFolder);
            var aft = new AudioFolderTransfer(Cfg.ReportPath, Cfg.ExistringFileListPath, Cfg.ErrorPath, waveWorkFolder)
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
                string intermediaPath = Path.Combine(waveWorkFolder, Guid.NewGuid() + ".wav");
                aft.ConvertToWave(Cfg.InputPath, intermediaPath, Cfg.OutputPath);
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
        private string WorkFolder = "";
        public AudioFolderTransfer(string reportPath, string existingFileListPath, string errorPath, string workFolder) : base()
        {
            ReportPath = reportPath;
            ExistingFileListPath = existingFileListPath;
            ErrorPath = errorPath;
            WorkFolder = workFolder;
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
            string intermediaPath = Path.Combine(WorkFolder, Guid.NewGuid() + ".wav");
            try
            {
                string ext = inputPath.Split('.').Last().ToLower();
                Sanity.Requires(ValidExtSet.Contains(ext), $"Invalid extension: {ext}");
                ConvertToWave(inputPath, intermediaPath, outputPath);
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
                            if (LocalCommon.AudioIdentical(existingFilePath, outputPath))
                            {
                                ErrorList.Add($"{outputPath}\t{existingFilePath}");
                                if (File.Exists(outputPath))
                                    File.Delete(outputPath);
                                return;
                            }
                        }
                        ExistingFileDict[key].Add(outputPath);
                    }
                    else
                        ExistingFileDict[key] = new List<string> { outputPath };
                    ReportList.Add(reportLine);
                    NewFileList.Add($"{key}\t{outputPath}");
                    if (File.Exists(intermediaPath))
                        File.Delete(intermediaPath);
                }
            }
            catch(CommonException e)
            {
                string errorMessage = $"{inputPath}\t{e.Message}";
                ErrorList.Add(errorMessage);
                Logger.WriteLineWithLock(errorMessage);
                if (File.Exists(intermediaPath))
                    File.Delete(intermediaPath);
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        public void ConvertToWave(string inputPath, string interMediaPath, string outputPath)
        {
            LocalCommon.SetAudioToWaveWithFfmpeg(inputPath.WrapPath(), interMediaPath.WrapPath());
            Wave w = new Wave();
            w.ShallowParse(interMediaPath);
            Sanity.Requires(w.SampleRate >= SampleRate, w.SampleRate.ToString());
            File.Delete(interMediaPath);
            LocalCommon.SetAudioWithFfmpeg(inputPath.WrapPath(), SampleRate, NumChannels, outputPath.WrapPath());            
        }

        public override string ItemRename(string inputItemName)
        {
            FileInfo file = new FileInfo(inputItemName);
            return file.Name.Replace(file.Extension, ".wav");
        }

        protected override void PostRun()
        {
            File.WriteAllLines(ReportPath, ReportList);
            File.WriteAllLines(ErrorPath, ErrorList);
            File.AppendAllLines(ExistingFileListPath, NewFileList);
        }
    }
}
