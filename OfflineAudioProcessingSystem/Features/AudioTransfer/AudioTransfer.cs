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
        Dictionary<string, string> Dict = new Dictionary<string, string>();
        protected override void LoadConfig(string configPath)
        {
            Cfg.Load(configPath);
        }
        private void GetNamingDict()
        {
            Dict = IO.ReadEmbed($"{LocalConstants.LOCAL_ASMB_NAME}.Internal.Data.FolderNameMapping.txt", LocalConstants.LOCAL_ASMB_NAME)
                .ToDictionary(x => x.Split('\t')[0], x => x.Split('\t')[1]);
        }
        protected override void Run()
        {
            GetNamingDict();
            foreach (string subPath in Cfg.InputAzureFolderPathArray)
                TransferFromAzureToAzure(AzureUtils.SetDataUri(subPath), Cfg.OutputAzureRootFolderPath);
        }       
        
        private void TransferFromAzureToAzure(string inputAzureUri, string outputAzureRootUri)
        {
            Console.WriteLine($"Processing {inputAzureUri}");
            var split = inputAzureUri.Trim('/').Split('/');
            string subFolderName = split.Last().Trim().Replace(":", "");
            string locale = Dict[split[split.Length - 2]];
            string downloadFolder = Path.Combine(WorkFolder, "Wave", subFolderName, "Download");
            string intermediaFolder = Path.Combine(WorkFolder, "Wave", subFolderName, "Intermedia");
            string uploadFolder = Path.Combine(Cfg.AudioRootFolder, locale, subFolderName);
            Directory.CreateDirectory(downloadFolder);
            Directory.CreateDirectory(intermediaFolder);
            Directory.CreateDirectory(uploadFolder);
            foreach(string azureFileName in AzureUtils.ListCurrentBlobs(inputAzureUri))
            {
                string fileName = azureFileName.Split('/').Last();
                string localPath = Path.Combine(downloadFolder, fileName);
                AzureUtils.Download(azureFileName, localPath);
            }

            string reportFolderPath = Path.Combine(Cfg.ReportRootFolderPath, "_Report", subFolderName);
            Directory.CreateDirectory(reportFolderPath);
            string reportPath = Path.Combine(reportFolderPath, "Report.txt");
            string errorPath = Path.Combine(reportFolderPath, "Error.txt");
            AudioFolderTransfer aft = new AudioFolderTransfer(reportPath, Cfg.ExistringFileListPath, errorPath, intermediaFolder)
            {
                SampleRate = Cfg.SampleRate,
                NumChannels = Cfg.NumChannels,
                MaxParallel = 5
            };
            aft.Run(downloadFolder, uploadFolder);

            foreach(string localFilePath in Directory.EnumerateFiles(uploadFolder))
            {
                string fileName = localFilePath.Split('\\').Last();
                string uploadFileName = AzureUtils.PathCombine(outputAzureRootUri, locale, subFolderName, fileName);
                AzureUtils.Upload(localFilePath, uploadFileName);
            }
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
                if (ext.ToLower() == ".ds_tore")
                    return;
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
