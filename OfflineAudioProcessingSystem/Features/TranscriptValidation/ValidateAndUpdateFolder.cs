using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Common;

namespace OfflineAudioProcessingSystem.TranscriptValidation
{
    class ValidateAndUpdateFolder
    {
        (string WrongTag, string CorrectTag)[] ReplaceArray = new (string WrongTag, string NewTag)[0];
        HashSet<string> ValidTagSet = new HashSet<string>();
        List<string> TextGridFormatErrorList = new List<string>();
        List<string> ErrorList = new List<string>();
        List<string> ReportList = new List<string>();
        List<string> TimeStampList = new List<string>();
        public ValidateAndUpdateFolder()
        {
            Init();
        }

        private void Init()
        {
            ReplaceArray = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.HighFreqErrorTagReplacement.txt", "OfflineAudioProcessingSystem")
                .Select(x => (x.Split('\t')[0], x.Split('\t')[1]))
                .ToArray();
            ValidTagSet = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.ValidTag.txt", "OfflineAudioProcessingSystem").ToHashSet();
        }

        public void ValidateAllFolders(bool transferToOutput=false)
        {
            string folderPath = @"F:\WorkFolder\Transcripts";
            List<string> annotatorList = new List<string>();
            List<string> reportList = new List<string>();
            foreach(string onlinePath in Directory.EnumerateDirectories(folderPath, "*online"))
            {
                Console.WriteLine(onlinePath);
                ValidateOnlineFolder(onlinePath, transferToOutput);
                annotatorList.Add(Path.Combine(onlinePath, "ToAnnotator.txt"));
                reportList.Add(Path.Combine(onlinePath, "AllFilter.txt"));
            }
            foreach(string offlinePath in Directory.EnumerateDirectories(folderPath, "*offline"))
            {
                Console.WriteLine(offlinePath);
                ValidateOfflineFolder(offlinePath,transferToOutput);
                annotatorList.Add(Path.Combine(offlinePath, "ToAnnotator.txt"));
                reportList.Add(Path.Combine(offlinePath, "AllFilter.txt"));
            }
            var aOutputList = annotatorList.SelectMany(x => File.ReadAllLines(x));
            File.WriteAllLines(Path.Combine(folderPath, "ToAnnotator.txt"), aOutputList);
            var rOutputList = reportList.SelectMany(x => File.ReadAllLines(x));
            File.WriteAllLines(Path.Combine(folderPath, "AllFilter.txt"), rOutputList);
        }
        public void ValidateAllFoldersTimeStamp()
        {
            string folderPath = @"F:\WorkFolder\Transcripts";
            List<string> timeStampList = new List<string>();
            foreach (string onlinePath in Directory.EnumerateDirectories(folderPath, "*online"))
            {
                TimeStampList = new List<string>();
                Console.WriteLine(onlinePath);
                ValidateOnlineFolderTimeStamp(onlinePath);
                timeStampList.Add(Path.Combine(onlinePath, "ToAnnotatorTimeStamp.txt"));
            }
            foreach (string offlinePath in Directory.EnumerateDirectories(folderPath, "*offline"))
            {
                TimeStampList = new List<string>();
                Console.WriteLine(offlinePath);
                ValidateOfflineFolderTimeStamp(offlinePath);
                timeStampList.Add(Path.Combine(offlinePath, "ToAnnotatorTimeStamp.txt"));
            }
            var outputList = timeStampList.SelectMany(x => File.ReadAllLines(x));
            File.WriteAllLines(Path.Combine(folderPath, "ToAnnotatorTimeStamp.txt"), outputList);
        }
       
        public void ValidateOnlineFolder(string folderPath, bool transferToOutput=false)
        {
            ErrorList = new List<string>();
            ReportList = new List<string>();
            string batchName = folderPath.Split('\\').Last();
                if (!new OneOffTotalMapping().SetByInputOnline(folderPath))
                    return;
                var list = File.ReadLines(Path.Combine(folderPath, "FromAnnotation.txt"))
                    .Select(x => new AnnotationLine(x));
                foreach (var line in list)
                    ValidateOnlineFile(folderPath, line, batchName,transferToOutput);
                File.WriteAllLines(Path.Combine(folderPath, "AllFilter.txt"), ErrorList);
                File.WriteAllLines(Path.Combine(folderPath, "ToAnnotator.txt"), ReportList);
        }

        public void ValidateOnlineFolderTimeStamp(string folderpath)
        {
            string batchName = folderpath.Split('\\').Last();
            if (!new OneOffTotalMapping().SetByInputOnline(folderpath))
                return;
            var list = File.ReadLines(Path.Combine(folderpath, "FromAnnotation.txt"))
                .Select(x => new AnnotationLine(x));
            foreach (var line in list)
                ValidateOnlineFileTimeStamp(folderpath, line, batchName);
            
            File.WriteAllLines(Path.Combine(folderpath, "ToAnnotatorTimeStamp.txt"), TimeStampList);
        }

        public void ValidateOnlineFileTimeStamp(string inputFolderPath, AnnotationLine line, string batchName)
        {
            string audioFilePath = Path.Combine(inputFolderPath, "Input", $"{line.TaskId}_{line.TaskName}", "Speaker", $"{line.AudioName}");
            string textFilePath = audioFilePath.Substring(0, audioFilePath.Length - 3) + "txt";
            if (!File.Exists(textFilePath))
                textFilePath = audioFilePath.Replace(".wav", ".txt");
            Sanity.Requires(File.Exists(textFilePath));
            ValidateAndUpdateFile v = new ValidateAndUpdateFile(ReplaceArray, ValidTagSet);
            v.ValidateTimeStamp(textFilePath, batchName, line.TaskName, line.AudioName, line.TaskId.ToString(), line.AudioPlatformId.ToString());
            TimeStampList.Add(v.TimeStampInfoString);
        }

        public void ValidateOnlineFile(string inputFolderPath, AnnotationLine line, string batchName, bool transferToOutput=false)
        {
            string audioFilePath = Path.Combine(inputFolderPath, "Input", $"{line.TaskId}_{line.TaskName}", "Speaker", $"{line.AudioName}");
            string textFilePath = audioFilePath.Substring(0, audioFilePath.Length - 3) + "txt";
            if (!File.Exists(textFilePath))
                textFilePath = audioFilePath.Replace(".wav", ".txt");
            Sanity.Requires(File.Exists(textFilePath));
            ValidateAndUpdateFile v = new ValidateAndUpdateFile(ReplaceArray, ValidTagSet);
            var output = v.ValidateAndCorrectFile(textFilePath, batchName, line.TaskName, line.AudioName, line.TaskId.ToString(), line.AudioPlatformId.ToString(), false, true);
            if (transferToOutput)
            {
                // 1 Too short.
                if (v.ErrorTypeArray[1])
                    return;
                string outputTextPath = textFilePath.Replace(@"\Input\", @"\Output\");
                string outputFolder = IO.GetFolder(outputTextPath);
                Directory.CreateDirectory(outputFolder);
                File.WriteAllLines(outputTextPath, output);
            }
            ErrorList.AddRange(v.ErrorList);
            ReportList.AddRange(v.ToAnnotatorList);
        }
        public void ValidateOfflineFolderTimeStamp(string folderPath)
        {
            string batchName = folderPath.Split('\\').Last();
            string inputPath = Path.Combine(folderPath, "Input");
            foreach (string textFilePath in Directory.EnumerateFiles(inputPath, "*.txt", SearchOption.AllDirectories))
            {
                string audioName = textFilePath.Substring(0, textFilePath.Length - 3) + "wav";
                ValidateAndUpdateFile v = new ValidateAndUpdateFile(ReplaceArray, ValidTagSet);
                v.ValidateTimeStamp(textFilePath, batchName, "Offline", audioName, "0", "0");
                TimeStampList.Add(v.TimeStampInfoString);
            }
            File.WriteAllLines(Path.Combine(folderPath, "ToAnnotatorTimeStamp.txt"), TimeStampList);
        }
        public void ValidateOfflineFolder(string folderPath, bool transferToOutput=false)
        {
            ErrorList = new List<string>();
            ReportList = new List<string>();
            string batchName = folderPath.Split('\\').Last();
            string inputPath = Path.Combine(folderPath, "Input");
            foreach (string textFilePath in Directory.EnumerateFiles(inputPath, "*.txt", SearchOption.AllDirectories))
            {
                string audioFilePath = textFilePath.Substring(0, textFilePath.Length - 3) + "wav";
                ValidateAndUpdateFile v = new ValidateAndUpdateFile(ReplaceArray, ValidTagSet);
                var output = v.ValidateAndCorrectFile(textFilePath, batchName, "Offline", audioFilePath, "0", "0", false, true);
                if (transferToOutput)
                {
                    // 1 Too short.
                    if (v.ErrorTypeArray[1])
                        continue;
                    string outputTextPath = textFilePath.Replace(@"\Input\", @"\Output\");
                    string outputFolder = IO.GetFolder(outputTextPath);
                    Directory.CreateDirectory(outputFolder);
                    File.WriteAllLines(outputTextPath, output);
                }
                ErrorList.AddRange(v.ErrorList);
                ReportList.AddRange(v.ToAnnotatorList);
            }
            File.WriteAllLines(Path.Combine(folderPath, "AllFilter.txt"), ErrorList);
            File.WriteAllLines(Path.Combine(folderPath, "ToAnnotator.txt"), ReportList);
        }
        public void ValidateAllTextGridFiles(bool overwrite=false)
        {
            List<string> list = new List<string>();
            foreach(string offlinePath in Directory.EnumerateDirectories(@"F:\WorkFolder\Transcripts", "*offline"))
            {
                ValidateTextGridFile(offlinePath, overwrite);
                list.AddRange(TextGridFormatErrorList);
                TransferTextFile(offlinePath);
            }
            string outputPath = Path.Combine(@"F:\WorkFolder\Transcripts", "Format.txt");
            File.WriteAllLines(outputPath, list);
        }
        public void ValidateTextGridFile(string folderPath, bool overwrite = false)
        {
            string textgridPath = Path.Combine(folderPath, "TextGrid");
            TextGridFormatErrorList = new List<string>();
            foreach (string textGridPath in Directory.EnumerateFiles(textgridPath, "*.textgrid", SearchOption.AllDirectories))
            {
                TextGridParser tp = new TextGridParser();
                tp.ExtractTextGridFile(textGridPath, overwrite);
                TextGridFormatErrorList.AddRange(tp.ErrorList);
            }
            string formatErrorPath = Path.Combine(folderPath, "Format.txt");
            File.WriteAllLines(formatErrorPath, TextGridFormatErrorList);
        }

        public void TransferTextFile(string folderPath)
        {
            string i = Path.Combine(folderPath, "Textgrid");
            string o = Path.Combine(folderPath, "Input");
            foreach(string taskFolder in Directory.EnumerateDirectories(i))
            {
                string taskName = taskFolder.Split('\\').Last();
                string targetFolder = Path.Combine(o, taskName, "Speaker");
                Directory.CreateDirectory(targetFolder);
                foreach(string filePath in Directory.EnumerateFiles(taskFolder, "*.txt"))
                {
                    string fileName = filePath.Split('\\').Last();
                    string outputFilePath = Path.Combine(targetFolder, fileName);
                    if (!File.Exists(outputFilePath))
                        File.Copy(filePath, outputFilePath, true);
                }
            }
        }
    }
}
