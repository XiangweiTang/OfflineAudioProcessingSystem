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
using System.Text.RegularExpressions;
using System.Collections;

namespace OfflineAudioProcessingSystem
{
    class Test
    {
        Random R = new Random();

        public Test(string[] args)
        {
            string path = @"D:\WorkFolder\Summary\20210222\Old_WithAnnotator.txt";
            var list = File.ReadLines(path)
                .Select(x => new OverallMappingLine(x));
            string dictPath = @"D:\WorkFolder\Summary\20210222\Dupe.txt";
            var dict = File.ReadLines(dictPath)
                .ToDictionary(x => x.Split('\t')[1], x => x.Split('\t')[0]);
            List<string> o = new List<string>();
            string oPath= @"D:\WorkFolder\Summary\20210222\Old_WithDupe.txt";
            foreach (var line in list)
            {
                string key = line.TaskId;
                string value = dict.ContainsKey(key) ? dict[key] : "-1";
                o.Add($"{line.Output()}\t{value}");
            }
            File.WriteAllLines(oPath, o);
        }

        private string GetKey(string s)
        {
            return s.Split('\t')[0];
        }
        class PathCompare : IEqualityComparer<IEnumerable<string>>
        {
            public bool Equals(IEnumerable<string> x, IEnumerable<string> y)
            {
                return x.Select(t => GetKey(t)).Intersect(y.Select(t => GetKey(t))).Count() > 0;
            }

            private string GetKey(string s)
            {
                return s.Split('\t')[9];
            }

            public int GetHashCode(IEnumerable<string> obj)
            {
                return 0;
            }
        }

        Regex NumberReg = new Regex(" ^[0-9]+$", RegexOptions.Compiled);


        private void DupeCheck(string checkFolder, params string[] oldFolders)
        {            
            var newList = Directory.EnumerateFiles(checkFolder, "*.wav", SearchOption.AllDirectories);
            var oldList = oldFolders.SelectMany(x => Directory.EnumerateFiles(x, "*.wav", SearchOption.AllDirectories));
            var list = LocalCommon.AudioIdenticalLocal(newList, oldList);
            File.WriteAllLines("Dupe.txt", list);
        }
        private void Calculate(string deliverfolder, HashSet<string> exclude)
        {
            List<string> list = new List<string>();
            double d = 0;
            double N = 0;
            foreach(string localePath in Directory.EnumerateDirectories(deliverfolder))
            {
                double totalTime = 0;
                string locale = localePath.Split('\\').Last();
                int n = 0;
                foreach(string wavpath in Directory.EnumerateFiles(localePath, "*.wav"))
                {
                    if (exclude.Contains(wavpath))
                        continue;
                    Wave w = new Wave();
                    w.ShallowParse(wavpath);
                    n++;
                    totalTime += w.AudioLength;
                }
                list.Add($"{locale}\t{n}\t{totalTime}\t{SecondsToHMS(totalTime)}");
                d += totalTime;
                N += n;
            }
            list.Add($"\t{N}\t{d}\t{SecondsToHMS(d)}");
            string reportPath = deliverfolder + "_Time.txt";
            File.WriteAllLines(reportPath, list);
        }

        private string SecondsToHMS(double d)
        {
            int i = (int)Math.Truncate(d);
            double f = d - i;
            int second = i % 60;            
            i /= 60;
            int minute = i % 60;
            i /= 60;
            return $"{i}h{minute:00}m{second:00}.{f:000}s";
        }
        private void DupeCheck(string workFolder, bool checkOnly)
        {
            string timeStamp = workFolder.Split('\\').Last();
            string inputFolderPath = Path.Combine(workFolder, "Output");
            string outputRootPath = @"D:\WorkFolder\Delivered";
            string outputFolderPath = Path.Combine(outputRootPath, timeStamp);
            string dupePath = outputFolderPath + ".txt";
            var dict = GetDupeDict();
            DupeCheckTransfer d = new DupeCheckTransfer(dict, dupePath, checkOnly);
            d.Run(inputFolderPath, outputFolderPath);
        }
        private void SetDummy(string i,string o)
        {
            Wave w = new Wave();
            w.ShallowParse(i);
            double l = w.AudioLength;
            int step = 8;
            Random r = new Random();
            List<string> list = new List<string>();
            for(int j = 0; j < l; j += step)
            {
                double start = j + r.NextDouble() * 3;
                double end = start + r.Next(5);
                if (end < l)
                    list.Add($"{start}\t{end}");
            }

            File.WriteAllLines(o, list);
        }
        private void MergeOfflineData(string workFolder)
        {
            string textgridFolder = Path.Combine(workFolder, "TextGrid");
            string audioFolder = Path.Combine(workFolder, "Audio");
            string mergeFolder = Path.Combine(workFolder, "Input");
            string reportPath = Path.Combine(workFolder, "TextgridMerge.txt");
            TranscriptValidation.TranscriptValidation t = new TranscriptValidation.TranscriptValidation();
            t.MergeTextGrid(textgridFolder, audioFolder, mergeFolder, reportPath);
        }
        Dictionary<string, string> NameIdDict = new Dictionary<string, string>();
        private void RunTransValidation(string workFolder)
        {
            string inputRootPath = Path.Combine(workFolder, "Input");            
            string blackListPath = Path.Combine(workFolder, "BlackList.txt");
            string blackListFilterPath = Path.Combine(workFolder, "BlackListFiltered.txt");
            string manuallyPath = Path.Combine(workFolder, "Manually.txt");
            string manuallFilterPath = Path.Combine(workFolder, "ManuallyFiltered.txt");
            string allPath = Path.Combine(workFolder, "All.txt");
            string allFilterPath = Path.Combine(workFolder, "AllFilter.txt");
            string annotatorPath = Path.Combine(workFolder, "FromAnnotation.txt");
            string reportPath = Path.Combine(workFolder, "Report.txt");
            var diff=Compare(annotatorPath, inputRootPath);
            if (diff.Length > 0)
            {
                foreach(string s in diff)
                    Console.WriteLine(s);
                return;
            }
            if (File.Exists(annotatorPath))
                GenerateNameIdDict(annotatorPath);

            TranscriptValidation.TranscriptValidation t = new TranscriptValidation.TranscriptValidation
            {
                BlackListPath = blackListPath,
                ManuallyPath = manuallyPath,
                InputRootPath = inputRootPath,
                AllPath = allPath,
            };
            t.RunValidation();
            var set = GetSetFromAnnotator(annotatorPath);
            FilterReport(set, blackListPath, blackListFilterPath);
            FilterReport(set, manuallyPath, manuallFilterPath);
            FilterAll(set, allPath, allFilterPath);
            GenerateAllReport(allFilterPath, reportPath);
        }
        private void RunTransUpdate(string workFolder)
        {
            string inputRootPath = Path.Combine(workFolder, "Input");
            string blackListPath = Path.Combine(workFolder, "BlackList.txt");
            string blackListFilterPath = Path.Combine(workFolder, "BlackListFiltered.txt");
            string manuallyPath = Path.Combine(workFolder, "Manually.txt");
            string manuallFilterPath = Path.Combine(workFolder, "ManuallyFiltered.txt");
            string outputRootPath = Path.Combine(workFolder, "Output");
            string mappingPath = Path.Combine(workFolder, "Mapping.txt");
            string annotatorPath = Path.Combine(workFolder, "FromAnnotation.txt");
            TranscriptValidation.TranscriptValidation t = new TranscriptValidation.TranscriptValidation
            {
                BlackListPath = blackListPath,
                ManuallyPath = manuallyPath,
                InputRootPath = inputRootPath,
                OutputRootPath=outputRootPath,
                MappingPath=mappingPath
            };
            HashSet<string> set = GetSetFromAnnotator(annotatorPath);
            t.RunUpdate(set,true);
        }
        private void FilterReport(HashSet<string> set, string i, string o)
        {   
            List<string> list = new List<string>();
            foreach(string s in File.ReadLines(i))
            {
                string filePath = s.Split('\t').Last();
                var split = filePath.Split('\\');
                string id = split[5].Split('_')[0];
                string name = split.Last().Replace(".txt", ".wav");
                string key = $"{id}\t{name}";
                if (set.Contains(key))
                    list.Add(s);
            }
            File.WriteAllLines(o, list);
        }

        private void FilterAll(HashSet<string> set, string i, string o)
        {
            List<string> list = new List<string>();
            foreach(string s in File.ReadLines(i))
            {
                string id = s.Split('\t')[1].Split('_')[0];
                string name = s.Split('\t')[2];
                string key = $"{id}\t{name}.wav";
                if (set.Contains(key))
                    list.Add(s);
            }
            File.WriteAllLines(o, list);
        }
        private void GenerateAllReport(string i, string o)
        {
            var list = File.ReadLines(i).Select(x => Reorg(x));
            File.WriteAllLines(o, list);
        }
        private string Reorg(string s)
        {
            var split = s.Split('\t');
            string taskId = split[1].Split('_')[0];
            string audioName = split[2];
            string audioId = NameIdDict[audioName];
            string errorMsg = split[3];            
            string xmin = split[4];
            string xmax = split.Length >= 6 ? split[5] : "";
            return $"{taskId}\t{audioId}\t{audioName}\t{errorMsg}\t{xmin}\t{xmax}";
        }
        private void GenerateNameIdDict(string annotatorPath)
        {
            NameIdDict = new Dictionary<string, string>();
            foreach(string s in File.ReadLines(annotatorPath))
            {
                var split = s.Split('\t');
                NameIdDict.Add(split[4].Split('.')[0], split[3]);
            }
        }
        private string[] Compare(string annotatorpath, string folderPath)
        {
            var set = GetSetFromAnnotator(annotatorpath);
            var dict = GetFiles(folderPath);
            return set.Except(dict.Keys).ToArray();
        }

        private Dictionary<string,string> GetFiles(string path)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach(string taskIdPath in Directory.EnumerateDirectories(path))
            {
                string id = taskIdPath.Split('\\').Last().Split('_')[0];
                string speakerPath = Path.Combine(taskIdPath, "speaker");
                foreach(string wavPath in Directory.EnumerateFiles(speakerPath, "*.wav"))
                {
                    string fileName = wavPath.Split('\\').Last();
                    string key = $"{id}\t{fileName}";
                    dict.Add(key, wavPath);
                }
            }
            return dict;
        }

        private HashSet<string> GetSetFromAnnotator(string path)
        {
            if (!File.Exists(path))
                return new HashSet<string>();
            return File.ReadLines(path)
                .Select(x => x.Split('\t'))
                .Select(x => $"{x[0]}\t{x[4]}")
                .ToHashSet();
        }

        private Dictionary<string,HashSet<string>> GetDupeDict()
        {
            string path = @"D:\WorkFolder\Delivered";
            Dictionary<string, HashSet<string>> dict = new Dictionary<string, HashSet<string>>();
            foreach(string filePath in Directory.EnumerateFiles(path, "*.wav", SearchOption.AllDirectories))
            {
                Wave w = new Wave();
                w.ShallowParse(filePath);
                string key = w.DataChunk.Length.ToString();
                if (!dict.ContainsKey(key))
                    dict[key] = new HashSet<string> { filePath };
                else
                    dict[key].Add(filePath);
            }
            return dict;
        }
    }
    class TextGridTransfer : FolderTransfer
    {
        public override string ItemRename(string inputItemName)
        {
            return inputItemName.Replace(".txt", ".textgrid");
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            TextGrid.TimeStampToTextGrid(inputPath, outputPath);
        }
    }
    class DupeCheckTransfer : FolderTransfer
    {
        Dictionary<string, HashSet<string>> Dict = new Dictionary<string, HashSet<string>>();
        List<string> DupeList = new List<string>();
        string DupePath = "";
        bool CheckOnly = true;
        public DupeCheckTransfer(Dictionary<string,HashSet<string>> dict, string dupePath, bool checkOnly) : base()
        {
            Dict = dict;
            DupePath = dupePath;
            CheckOnly = checkOnly;
        }
        protected override IEnumerable<string> GetItems(string inputFolderPath)
        {
            return Directory.EnumerateFiles(inputFolderPath, "*.wav");
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            Wave w = new Wave();
            w.ShallowParse(inputPath);
            string key = w.DataChunk.Length.ToString();
            if (Dict.ContainsKey(key))
            {
                foreach(string existingPath in Dict[key])
                {
                    if (LocalCommon.AudioIdenticalLocal(existingPath, inputPath))
                    {
                        DupeList.Add(inputPath + "\t" + existingPath);
                        return;
                    }
                }
            }
            if (!CheckOnly)
            {
                File.Copy(inputPath, outputPath);
                string iTextPath = inputPath.Replace(".wav", ".txt");
                string oTextPath = outputPath.Replace(".wav", ".txt");
                File.Copy(iTextPath, oTextPath);
            }
        }

        protected override void PostRun()
        {
            File.WriteAllLines(DupePath, DupeList);
        }
    }
    class TimeStampTransfer : FolderTransfer
    {
        protected override IEnumerable<string> GetItems(string inputFolderPath)
        {
            return Directory.EnumerateFiles(inputFolderPath, "*.wav");
        }
        public override string ItemRename(string inputItemName)
        {
            FileInfo file = new FileInfo(inputItemName);
            return inputItemName.Replace(file.Extension, ".txt");
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            LocalCommon.SetTimeStampsWithVad(inputPath, outputPath);
        }
    }
    class Rename : FolderTransfer
    {        
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            File.Copy(inputPath, outputPath);
        }
        public override string ItemRename(string inputItemName)
        {
            return inputItemName.Replace('+', '_');
        }
    }

    class RunAudioTransfer
    {
        // TimeStamp/locale/Speaker
        string reportRootFolder = @"D:\Tmp\_Report";
        string TimeStamp= DateTime.Now.ToString("yyyyMMdd_hhmmss");
        public string InputRootFolder { get; set; } = @"D:\Tmp\20210219";
        string workRootFolder { get; set; } = @"D:\WorkFolder\OverallTmp";
        string dailyRootFolder= @"D:\WorkFolder\DailyFolder";
        string existingPath = @"D:\WorkFolder\Input\Summary.txt";
        string outputRootFolder = @"D:\WorkFolder\Input\300hrsRecordingContent";
        public void Run()
        {
            string inputTimeStamp = InputRootFolder.Split('\\').Last();
            foreach(string localeFolder in Directory.EnumerateDirectories(InputRootFolder))
            {
                string locale = localeFolder.Split('\\').Last();
                foreach(string speakerIdFolder in Directory.EnumerateDirectories(localeFolder))
                {
                    string speakerId = speakerIdFolder.Split('\\').Last();
                    
                    string reportFolder = Path.Combine(reportRootFolder, TimeStamp, locale, speakerId);
                    Directory.CreateDirectory(reportFolder);
                    string reportPath = Path.Combine(reportFolder, "report.txt");
                    string errorPath = Path.Combine(reportFolder, "error.txt");
                    string workFolder = Path.Combine(workRootFolder, inputTimeStamp, locale, speakerId);
                    Directory.CreateDirectory(workFolder);
                    string outputFolder = Path.Combine(outputRootFolder, inputTimeStamp, locale, speakerId);
                    Directory.CreateDirectory(outputFolder);
                    AudioTransfer.AudioFolderTransfer aft = new AudioTransfer.AudioFolderTransfer(reportPath, existingPath, errorPath, workFolder);
                    aft.Run(speakerIdFolder, outputFolder);

                    string dailyFolder = Path.Combine(dailyRootFolder, TimeStamp, $"{inputTimeStamp}_{locale}_{speakerId}");
                    Directory.CreateDirectory(dailyFolder);
                    FolderCopy fc = new FolderCopy();
                    fc.Run(outputFolder, dailyFolder);
                }
            }
        }
    }
        

    class BetaFeature
    {
        public string Input { get; set; } = @"D:\Tmp\20210126_Audio\Visp";
        public string workRootFolder { get; set; } = @"D:\WorkFolder\OverallTmp";
        string o = @"D:\WorkFolder\Input\300hrsRecordingContent";
        string d = @"D:\WorkFolder\DailyFolder";
        public void Run()
        {
            string reportRootFolder = @"D:\Tmp\_Report";
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_hhmmss");


            foreach (string timeStampPath in Directory.EnumerateDirectories(Input))
            {
                string locale = Input.GetLastNPart('\\');
                string t = timeStampPath.GetLastNPart('\\');
                foreach(string speakerIdPath in Directory.EnumerateDirectories(timeStampPath))
                {
                    string speakerId = speakerIdPath.GetLastNPart('\\');
                    string reportRootPath = Path.Combine(reportRootFolder, timeStamp, t, locale, speakerId);
                    string reportPath = Path.Combine(reportRootPath, "Report.txt");
                    string errorPath = Path.Combine(reportRootPath, "Error.txt");
                    string workFolderPath = Path.Combine(workRootFolder, t, locale, speakerId);
                    Directory.CreateDirectory(reportRootPath);
                    Directory.CreateDirectory(workFolderPath);
                    AudioTransfer.AudioFolderTransfer aft = new AudioTransfer.AudioFolderTransfer(reportPath, @"D:\WorkFolder\Input\Summary.txt", errorPath, workFolderPath);
                    string outputPath = Path.Combine(o, t, locale, speakerId);
                    Directory.CreateDirectory(outputPath);
                    aft.Run(speakerIdPath, outputPath);
                    string dailyFolder = Path.Combine(d, timeStamp, $"{locale}_{speakerId}");
                    FolderCopy fc = new FolderCopy();
                    fc.Run(outputPath, dailyFolder);
                }
            }
            
        }
    }   



    class ATransfer : FolderTransfer
    {
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            LocalCommon.SetAudioToWaveWithFfmpeg(inputPath, outputPath);
        }
    }

    class PlatformLine : Line
    {
        public PlatformLine(string lineStr) : base(lineStr)
        {
        }

        public int TaskID { get; set; }
        public string TaskName { get; set; }
        public string TaskStatus { get; set; }
        public int AudioID { get; set; }
        public string AudioName { get; set; }
        public string AudioStatus { get; set; }
        public string Annotator { get; set; }
        public string Duration { get; set; }
        public string ValidDuration { get; set; }
        public string PassDuration { get; set; }
        public string PassValidDuration { get; set; }

        protected override IEnumerable<object> GetLine()
        {
            yield return TaskID;
            yield return TaskName;
            yield return TaskStatus;
            yield return AudioID;
            yield return AudioName;
            yield return AudioStatus;
            yield return Annotator;
            yield return Duration;
            yield return ValidDuration;
            yield return PassDuration;
            yield return PassValidDuration;
        }

        protected override void SetLine(string[] split)
        {
            TaskID = int.Parse(split[0].Trim());
            TaskName = split[1].Trim();
            TaskStatus = split[2].Trim();
            AudioID = int.Parse(split[3].Trim());
            AudioName = split[4].Trim();
            AudioStatus = split[5].Trim();
            Annotator = split[6].Trim();
            Duration = split[7].Trim();
            ValidDuration = split[8].Trim();
            PassDuration = split[9].Trim();
            PassValidDuration = split[10].Trim();
        }
    }    

    class AudioMappingLine:Line
    {
        public AudioMappingLine(string s) : base(s) { }
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string AudioId { get; set; }
        public string WavName { get; set; }
        public string SpeakerId { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        protected override IEnumerable<object> GetLine()
        {
            yield return TaskId;
            yield return TaskName;
            yield return AudioId;
            yield return WavName;
            yield return SpeakerId;
            yield return Gender;
            yield return Age;
        }

        protected override void SetLine(string[] split)
        {
            TaskId = split[0];
            TaskName = split[1];
            AudioId = split[2];
            WavName = split[3];
            SpeakerId = split[4];
            Gender = split[5];
            Age = split[6];
        }
    }
    class OverallMappingLine : Line
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string AudioId { get; set; }
        public string AudioName { get; set; }
        public string Speaker { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string Dialect { get; set; }
        public string AudioFolder { get; set; }
        public string AudioPath { get; set; }
        public string TeamName { get; set; }
        public OverallMappingLine(string lineStr) : base(lineStr)
        {
        }

        protected override IEnumerable<object> GetLine()
        {
            yield return TaskId;
            yield return TaskName;
            yield return AudioId;
            yield return AudioName;
            yield return Speaker;
            yield return Gender;
            yield return Age;
            yield return Dialect;
            yield return AudioFolder;
            yield return AudioPath;
            yield return TeamName;
        }

        protected override void SetLine(string[] split)
        {
            TaskId = split[0];
            TaskName = split[1];
            AudioId = split[2];
            AudioName = split[3];
            Speaker = split[4];
            Gender = split[5];
            Age = split[6];
            Dialect = split[7];
            AudioFolder = split[8];
            AudioPath = split[9];
            TeamName = split[10];
            if (string.IsNullOrWhiteSpace(TeamName))
                TeamName = "NotAssigned";
        }
    }
}
