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
        List<string> TotalReportList = new List<string>();
        List<string> TotalErrorList = new List<string>();
        Dictionary<string, string> LocaleRegDict = new Dictionary<string, string>();
        protected override void LoadConfig(string configPath)
        {
            Cfg.Load(configPath);
        }
        private void Init()
        {
            Dict = IO.ReadEmbed($"{LocalConstants.LOCAL_ASMB_NAME}.Internal.Data.FolderNameMapping.txt", LocalConstants.LOCAL_ASMB_NAME)
                .ToDictionary(x => x.Split('\t')[0], x => x.Split('\t')[1]);
            LocaleRegDict= IO.ReadEmbed($"{LocalConstants.LOCAL_ASMB_NAME}.Internal.Data.LocaleRegexMapping.txt", LocalConstants.LOCAL_ASMB_NAME)
                .ToDictionary(x => x.Split('\t')[0], x => x.Split('\t')[1]);
        }

        //public void OneOffRun(string inputUri, string outputRootFolder, )
        protected override void Run()
        {
            Init();
            TotalReportList.Add("Original name\tWave name\tDuration(s)");
            foreach (string subPath in Cfg.InputAzureFolderPathArray)
                TransferFromAzureToAzure(AzureUtils.GetFullUriString(subPath), Cfg.OutputAzureRootFolderPath, Cfg.DailyRootFolderPath);
            string errorPath = Cfg.ReportRootFolderPath + ".error";
            string reportPath = Cfg.ReportRootFolderPath + ".log";
            File.WriteAllLines(errorPath, TotalErrorList);
            File.WriteAllLines(reportPath, TotalReportList);
        }       
        
        private void TransferFromAzureToAzure(string inputAzureUri, string outputAzureRootUri, string localDailyRootFolder)
        {
            Console.WriteLine($"Processing {inputAzureUri}");

            var split = inputAzureUri.Trim('/').Split('/');
            string subFolderName = split.Last().Trim().Replace(":", "");
            string locale = Dict[split[split.Length - 2]];

            string downloadFolder = Path.Combine(WorkFolder, "Wave", subFolderName, "Download");
            string intermediaFolder = Path.Combine(WorkFolder, "Wave", subFolderName, "Intermedia");
            string uploadFolder = Path.Combine(Cfg.AudioRootFolder, locale, subFolderName);
            string localDailyFolder = Path.Combine(localDailyRootFolder, subFolderName);
            string reportFolderPath = Path.Combine(Cfg.ReportRootFolderPath, subFolderName);
            string reportFilePath = Path.Combine(reportFolderPath, "Report.txt");
            string errorFilePath = Path.Combine(reportFolderPath, "Error.txt");
            string outputAzureUri = AzureUtils.PathCombine(outputAzureRootUri, locale, subFolderName);

            Directory.CreateDirectory(downloadFolder);
            Directory.CreateDirectory(intermediaFolder);
            Directory.CreateDirectory(uploadFolder);
            Directory.CreateDirectory(localDailyFolder);
            Directory.CreateDirectory(reportFolderPath);

            Download(inputAzureUri, downloadFolder);

            CheckAndTransfer(reportFilePath, errorFilePath, downloadFolder, intermediaFolder, uploadFolder);
            Copy(uploadFolder, localDailyFolder);

            Upload(uploadFolder, outputAzureUri);
        }
        public void NewDownload(string rootUri, string blobContainerName,string rootPath)
        {
            foreach(string localeUri in AzureUtils.ListCurrentDirectories(rootUri))
            {
                string locale;
                try
                {
                    locale = localeUri.GetLastNPart('/');
                }
                catch
                {
                    continue;
                }
                foreach(string timeStampUri in AzureUtils.ListCurrentDirectories(localeUri))
                {
                    string timeStamp = timeStampUri.GetLastNPart('/');
                    DateTime dt = DateTime.Parse(timeStamp);
                    timeStamp = dt.ToString("yyyyMMdd");
                    foreach (string speakerIdUri in AzureUtils.ListCurrentDirectories(timeStampUri))
                    {
                        string speakerId = timeStampUri.GetLastNPart('/');
                        string localFolderPath = Path.Combine(rootPath, locale, timeStamp, speakerId);
                        Directory.CreateDirectory(localFolderPath);
                        foreach (string azureFilePath in AzureUtils.ListCurrentBlobs(speakerIdUri))
                        {
                            string fileName = azureFilePath.GetLastNPart('/');
                            string localFilePath = Path.Combine(localFolderPath, fileName);
                            AzureUtils.DownloadFile(azureFilePath, localFilePath);
                        }
                    }
                }
            }
        }
        private void Download(string inputAzureUri, string downloadFolder)
        {
            foreach (string azureFileName in AzureUtils.ListCurrentBlobs(inputAzureUri))
            {
                string fileName = azureFileName.Split('/').Last();
                string localPath = Path.Combine(downloadFolder, fileName);
                AzureUtils.DownloadFile(azureFileName, localPath);
            }
        }

        public void CheckAndTransfer(string reportPath, string errorPath, string downloadFolder, string intermediaFolder, string uploadFolder)
        {
            AudioFolderTransfer aft = new AudioFolderTransfer(reportPath, Cfg.ExistringFileListPath, errorPath, intermediaFolder)
            {
                SampleRate = Cfg.SampleRate,
                NumChannels = Cfg.NumChannels,
                MaxParallel = 5
            };
            aft.Run(downloadFolder, uploadFolder);            
            TotalReportList.AddRange(aft.ReportList);
            TotalErrorList.AddRange(aft.ErrorList);
        }

        private void Upload(string uploadFolder, string outputAzureUri)
        {
            foreach (string localFilePath in Directory.EnumerateFiles(uploadFolder))
            {
                string fileName = localFilePath.Split('\\').Last();
                string uploadFileName = AzureUtils.PathCombine(outputAzureUri, fileName);
                AzureUtils.Upload(localFilePath, uploadFileName);
            }
        }     
        private void Copy(string uploadFolder, string localDailyFolder)
        {
            FolderCopy fc = new FolderCopy();
            fc.Run(uploadFolder, localDailyFolder);
        }
    }

    class AudioFolderTransfer : FolderTransfer
    {
        HashSet<string> ValidExtSet = new HashSet<string>();
        public int SampleRate { get; set; } = 16000;
        public int NumChannels { get; set; } = 1;
        public List<string> ReportList { get; private set; } = new List<string>();
        public List<string> ErrorList { get; private set; } = new List<string>();
        private string ReportPath = "";
        private string ErrorPath = "";
        private Dictionary<string, List<string>> FileSizeDict = new Dictionary<string, List<string>>();
        private Dictionary<string, string> ExistingFileDict = new Dictionary<string, string>();
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
            ValidExtSet = IO.ReadEmbed($"{LocalConstants.LOCAL_ASMB_NAME}.Internal.Data.AudioInputExt.txt", LocalConstants.LOCAL_ASMB_NAME)
                .ToHashSet();
            FileSizeDict = File.ReadLines(ExistingFileListPath)
                .GroupBy(x => x.Split('\t')[0])
                .ToDictionary(x => x.Key, x => x.Select(y => y.Split('\t')[1]).ToList());
            ExistingFileDict = File.ReadLines(ExistingFileListPath)
                .ToDictionary(x => x.Split('\t')[1], x => x.Split('\t')[0]);
        }
        
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            string intermediaPath = Path.Combine(WorkFolder, Guid.NewGuid() + ".wav");
            try
            {
                if (ExistingFileDict.ContainsKey(outputPath))
                {
                    Console.WriteLine($"File exists: {outputPath}");
                    return;
                }
                string ext = inputPath.Split('.').Last().ToLower();
                if (ext.ToLower() == "ds_store" || ext.ToLower() == "pdf")
                    return;
                Sanity.Requires(ValidExtSet.Contains(ext), $"Invalid extension: {ext}");
                ConvertToWave(inputPath, intermediaPath, outputPath);
                Wave w = new Wave();
                w.ShallowParse(outputPath);
                string reportLine = $"{inputPath.Split('\\').Last()}\t{outputPath.Split('\\').Last()}\t{w.AudioLength}";
                string key = w.DataChunk.Length.ToString();
                lock (LockObj)
                {
                    if (FileSizeDict.ContainsKey(key))
                    {
                        foreach (string existingFilePath in FileSizeDict[key])
                        {
                            if (LocalCommon.AudioIdenticalLocal(existingFilePath, outputPath))
                            {
                                ErrorList.Add($"{outputPath}\t{existingFilePath}");
                                if (File.Exists(outputPath))
                                    File.Delete(outputPath);
                                return;
                            }
                        }
                        FileSizeDict[key].Add(outputPath);
                    }
                    else
                        FileSizeDict[key] = new List<string> { outputPath };
                    try
                    {
                        string outputTimeStampPath = outputPath.Replace(".wav", ".txt");
                        if (File.Exists(outputTimeStampPath))
                            File.Delete(outputTimeStampPath);
                        LocalCommon.SetTimeStampsWithVad(outputPath, outputTimeStampPath);
                        Sanity.Requires(File.Exists(outputTimeStampPath));
                        string outputTextGridPath = outputPath.Replace(".wav", ".textgrid");
                        if (File.Exists(outputTextGridPath))
                            File.Delete(outputTextGridPath);
                        TextGrid.TimeStampToTextGrid(outputTimeStampPath, outputTextGridPath);
                    }
                    catch
                    {
                        ErrorList.Add($"{outputPath}\tNo time stamp file.");
                    }
                    ReportList.Add(reportLine);
                    NewFileList.Add($"{key}\t{outputPath}");
                    if (File.Exists(intermediaPath))
                        File.Delete(intermediaPath);
                }
            }
            catch (CommonException e)
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
            return file.Name.Replace(file.Extension, ".wav").Replace("..", ".").Replace("%3A", "_");
        }

        protected override void PostRun()
        {
            File.WriteAllLines(ReportPath, ReportList);
            File.WriteAllLines(ErrorPath, ErrorList);
            File.AppendAllLines(ExistingFileListPath, NewFileList);
        }
    }    
}
