using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Common;

namespace OfflineAudioProcessingSystem
{
    class FullMapping
    {
        public FullMapping() { }
        public void Run()
        {
            CreateMetaDataWave();
        }
        Regex NumberReg = new Regex("^[0-9_]+$", RegexOptions.Compiled);
        public void OutputRecordingStatus(string inputPath)
        {
            Dictionary<string, double> dict = new Dictionary<string, double>();
            foreach (string dailyPath in Directory.EnumerateDirectories(inputPath))
            {
                string folderName = dailyPath.Split('\\').Last().ToLower();
                if (NumberReg.IsMatch(folderName))
                {
                    foreach (string localePath in Directory.EnumerateDirectories(dailyPath))
                    {
                        string locale = localePath.Split('\\').Last().ToLower();
                        foreach (string speakerIdPath in Directory.EnumerateDirectories(localePath))
                        {
                            string speakerId = speakerIdPath.Split('\\').Last();
                            if (NumberReg.IsMatch(speakerId))
                            {
                                string key = $"Team2_{locale}";
                                if (!dict.ContainsKey(key))
                                    dict[key] = 0;
                                dict[key] += GetFolderHours(speakerIdPath);
                            }
                            else
                            {
                                string key = $"Team1_{locale}";
                                if (!dict.ContainsKey(key))
                                    dict[key] = 0;
                                dict[key] += GetFolderHours(speakerIdPath);
                            }
                        }
                    }
                }
                else
                {
                    string key = $"Team1_{folderName}";
                    if (!dict.ContainsKey(key))
                        dict[key] = 0;
                    dict[key] += GetFolderHours(dailyPath);
                }
            }
            double d = 0;
            foreach (var item in dict.OrderBy(x => x.Key))
            {
                Console.WriteLine($"{item.Key}\t{item.Value}\t{item.Value / 3600}");
                d += item.Value;
            }
            Console.WriteLine($"All\t{d}\t{d / 3600}");
        }
        private double GetFolderHours(string path)
        {
            return Directory.EnumerateFiles(path, "*.wav", SearchOption.AllDirectories)
                .Sum(x =>
                {
                    Wave w = new Wave();
                    w.ShallowParse(x);
                    return w.AudioLength;
                });
        }
        private void ExpendOldInfoDict(string outputPath)
        {
            var dict = GetOldInfoDict();
            var list = GetOnlineFromExcel(@"f:\Tmp\Online20210308.txt");
            foreach(var line in list)
            {
                if (dict.ContainsKey(line.AudioPlatformId.ToString()))
                    continue;
                OverallMappingLine newLine = new OverallMappingLine
                {
                    AudioId = line.AudioPlatformId.ToString(),
                    TaskId = line.TaskId.ToString(),
                    TaskName = line.TaskName,
                    AudioName = line.AudioName,
                };
                dict.Add(line.AudioPlatformId.ToString(), newLine);
            }
            File.WriteAllLines(outputPath, dict.Select(x => x.Value.Output()));
        }
        private Dictionary<string, OverallMappingLine> GetOldInfoDict()
        {
            string path = @"f:\WorkFolder\Summary\20210222\Important\Old_WithSR.txt";
            return File.ReadLines(path)
                .Select(x => new OverallMappingLine(x))
                .ToDictionary(x => x.AudioId, x => x);
        }
        private IEnumerable<string> GetAllLocalFiles()
        {
            string rootPath = @"f:\WorkFolder\Input\300hrsRecordingContent";
            return Directory.EnumerateFiles(rootPath, "*.wav", SearchOption.AllDirectories)
                .Select(x => x.ToLower());
        }
        private IEnumerable<AnnotationLine> GetOnlineFromExcel(string path)
        {
            return File.ReadLines(path)
                .Select(x => new AnnotationLine(x));
        }
        public void CreateMetaDataWave()
        {
            CreateMetaData(@"f:\WorkFolder\300hrsRecordingNew", "*.wav", @"f:\WorkFolder\300hrsRecordingNew\Recording.metadata.txt");
        }
        public void CreateMetaDataText()
        {
            CreateMetaData(@"f:\WorkFolder\300hrsAnnotationNew", "*.txt", @"f:\WorkFolder\300hrsAnnotationNew\Annotation.metadata.txt");
        }
        private void CreateMetaData(string folderPath,string pattern, string outputPath)
        {
            List<string> list = new List<string>();
            foreach(string localePath in Directory.EnumerateDirectories(folderPath))
            {
                string locale = localePath.Split('\\').Last();
                foreach(string speakerIdPath in Directory.EnumerateDirectories(localePath))
                {
                    string speaker = speakerIdPath.Split('\\').Last();
                    string recordedBy = speaker[0] == '0' ? "Team1" : "Team2";
                    foreach(string filePath in Directory.EnumerateFiles(speakerIdPath, pattern))
                    {
                        string fileName = filePath.Split('\\').Last();
                        MetaDataLine line = new MetaDataLine
                        {
                            SpeakerId = speaker,
                            Locale = locale,
                            AudioId = fileName.Split('.')[0],
                            AnnotatedBy = "Team2",
                            RelativePath = $"{locale}/{speaker}/{fileName}",
                            RecordedBy = recordedBy
                        };
                        list.Add(line.Output());
                    }
                }
            }
            File.WriteAllLines(outputPath, list);
        }
        public void CreateSpeakerInfoFile()
        {
            string inputPath = @"f:\WorkFolder\Summary\20210222\Important\Iddict.txt";
            string outputPath = @"f:\WorkFolder\Summary\20210222\Important\SpeakerInfo.txt";
            var list = File.ReadLines(inputPath)
                .Select(x => new IdDictLine(x))
                .Select(x => $"{x.UniversalId}\t{x.MergedId.Split('_')[0]}\t{x.Gender}\t{x.Age}");
            File.WriteAllLines(outputPath, list);
        }
        public void CalculateDeliverAudioInfo()
        {
            List<string> list = new List<string>();
            double total = 0;
            foreach(string localePath in Directory.EnumerateDirectories(@"F:\WorkFolder\300hrsRecordingNew"))
            {
                string locale = localePath.Split('\\').Last();
                double r = Directory.EnumerateFiles(localePath, "*.wav", SearchOption.AllDirectories)
                    .Sum(x =>
                    {
                        Wave w = new Wave();
                        w.ShallowParse(x);
                        return w.AudioLength;
                    });
                total += r;
                list.Add($"{locale}\t{r:0.00}\t{r / 3600:0.00}");
            }
            list.Add($"Total\t{total:0.00}\t{total / 3600:0.00}");
            File.WriteAllLines(@"F:\WorkFolder\300hrsRecordingNew\Report.txt", list);
        }
        
        public void CalculateDeliverTextInfo()
        {
            string totalPath = @"F:\WorkFolder\Summary\20210222\Important\TransMapping.txt";
            var dict = File.ReadLines(totalPath)
                .ToDictionary(x => x.Split('\t')[2].ToLower(), x => x.Split('\t')[3]);
            Dictionary<string, double> timeDict = new Dictionary<string, double>();
            double total = 0;
            List<string> outputList = new List<string>();
            foreach(string localePath in Directory.EnumerateDirectories(@"F:\WorkFolder\300hrsAnnotationNew"))
            {
                string locale = localePath.Split('\\').Last();
                timeDict[locale] = 0;
                foreach(string path in Directory.EnumerateFiles(localePath, "*.txt", SearchOption.AllDirectories))
                {
                    string audioPath = dict[path.ToLower()];
                    Wave w = new Wave();
                    w.ShallowParse(audioPath);
                    timeDict[locale] += w.AudioLength;
                    total += w.AudioLength;
                }
                outputList.Add($"{locale}\t{timeDict[locale]}\t{timeDict[locale] / 3600}");
            }
            outputList.Add($"Total\t{total}\t{total / 3600}");
            File.WriteAllLines(@"F:\WorkFolder\300hrsAnnotationNew\Report.txt", outputList);
        }

        #region Output online data
        public void MappingOnlineDataAll(string rootPath)
        {
            List<string> list = new List<string>();
            foreach(string onlineFolder in Directory.EnumerateDirectories(rootPath, " * online"))
            {
                list.AddRange(File.ReadLines(Path.Combine(onlineFolder, "OutputMapping.txt")));
            }
            string outputPath = Path.Combine(rootPath, "OnlineMapping.txt");
            File.WriteAllLines(outputPath, list);
        }
        public void MappingOnlineData(string workFolder)
        {
            string outputFolder = Path.Combine(workFolder, "Output");
            string overallMappingPath = @"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt";
            var taskIdNameDict = File.ReadLines(overallMappingPath)
                .Select(x => new OverallMappingLine(x))
                .Select(x => new { x.AudioFolder, x.TaskId })
                .Where(x=>x.TaskId!="0"&&x.TaskId!=""&&x.TaskId!="627")
                .Distinct()
                .ToDictionary(x => x.TaskId, x => x.AudioFolder.ToLower());
            List<string> outputList = new List<string>();
            string outputPath = Path.Combine(workFolder, "OutputMapping.txt");
            foreach(string taskFolder in Directory.EnumerateDirectories(outputFolder))
            {
                string taskId = taskFolder.Split('\\').Last().Split('_')[0];
                string value = taskIdNameDict[taskId];
                string speakerFolder = Path.Combine(taskFolder, "Speaker");
                var speakerDict = RawMapping(speakerFolder);
                var realDict = RawMapping(value);
                foreach(string audioPath in Directory.EnumerateFiles(speakerFolder, "*.wav"))
                {
                    string textPath = audioPath.Replace(".wav", ".txt");
                    if (!File.Exists(textPath))
                        textPath = "";
                    string audioName = audioPath.Split('\\').Last();
                    string key = GetRawKey(audioName);
                    if (realDict.ContainsKey(key))
                    {
                        if (realDict[key] != "")
                            Sanity.Requires(LocalCommon.AudioIdenticalLocal(realDict[key], audioPath));
                        outputList.Add($"{audioPath}\t{textPath}\t{realDict[key]}\t{value}");
                    }
                    else
                        outputList.Add($"{audioPath}\t{textPath}\t\t{value}");
                }
            }
            File.WriteAllLines(outputPath, outputList);
        }
        #endregion

        private Dictionary<string,string> RawMapping(string folderPath)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach(string audioPath in Directory.EnumerateFiles(folderPath, "*.wav"))
            {
                string key = GetRawKey(audioPath.Split('\\').Last().ToLower());
                if (dict.ContainsKey(key))
                    dict[key] = "";
                else
                    dict[key] = audioPath;
            }
            return dict;
        }

        private string GetRawKey(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach(char c in s.ToLower())
            {
                if ('0' <= c && c <= '9')
                {
                    sb.Append(c);
                    continue;
                }
                if ('a' <= c && c <= 'z')
                {
                    sb.Append(c);
                    continue;
                }
            }
            return sb.ToString();
        }
        public void MappingOfflineDataAll(string rootPath)
        {
            List<string> list = new List<string>();
            foreach(string offlineFolder in Directory.EnumerateDirectories(rootPath, "*offline"))
            {
                list.AddRange(File.ReadLines(Path.Combine(offlineFolder, "OutputMapping.txt")));
            }
            string outputPath = Path.Combine(rootPath, "OfflineMapping.txt");
            File.WriteAllLines(outputPath, list);
        }
        public void ProduceOfflineFolderMapping(string workFolder)
        {
            string outputFolder = Path.Combine(workFolder, "Output");
            string outputPath = Path.Combine(workFolder, "OutputFolderMapping.txt");
            var list = Directory.EnumerateDirectories(outputFolder);
            File.WriteAllLines(outputPath, list);
        }

        public void MappingOffLineData(string workFolder)
        {
            string outputPath = Path.Combine(workFolder, "OutputMapping.txt");
            var folderMappingDict = File.ReadLines(Path.Combine(workFolder, "OutputFolderMapping.txt"))
                .ToDictionary(x => x.Split('\t')[0], x => x.Split('\t')[1]);
            List<string> outputList = new List<string>();
            string outputFolderPath = Path.Combine(workFolder, "Output");
            foreach(string taskFolder in Directory.EnumerateDirectories(outputFolderPath))
            {
                string realFolder = folderMappingDict[taskFolder];
                var realDict = RawMapping(realFolder);
                string speakerFolder = Path.Combine(taskFolder, "Speaker");
                foreach(string audioPath in Directory.EnumerateFiles(speakerFolder,"*.wav"))
                {
                    string audioName = audioPath.Split('\\').Last();
                    string key = GetRawKey(audioName);
                    string s;
                    string textPath = audioPath.Replace(".wav", ".txt");
                    if (!File.Exists(textPath))
                        textPath = "";
                    if (realDict.ContainsKey(key))
                    {
                        string realPath = realDict[key];
                        if (realPath != "")
                            Sanity.Requires(LocalCommon.AudioIdenticalLocal(realPath, audioPath));
                        s = $"{audioPath}\t{textPath}\t{realPath}\t{taskFolder}";
                    }
                    else
                    {
                        s = $"{audioPath}\t{textPath}\t\t{taskFolder}";
                    }
                    outputList.Add(s);
                }
            }
            File.WriteAllLines(outputPath, outputList);
        }

        public void MappingTextFilesToDeliver()
        {
            string offLinePath = @"F:\WorkFolder\Transcripts\OfflineMapping.txt";
            string onlinePath = @"F:\WorkFolder\Transcripts\OnlineMapping.txt";
            string overallMappingPath = @"F:\WorkFolder\Summary\20210222\Important\FullMapping.txt";
            string outputPath = @"F:\WorkFolder\Summary\20210222\Important\TransMapping.txt";
            var dict = File.ReadLines(overallMappingPath)
                .Select(x => new FullMappingLine(x))
                .Select(x => new { x.OldPath, x.NewPath })
                .Distinct()
                .ToDictionary(x => x.OldPath.ToLower(),x=>x.NewPath);

            var list = File.ReadLines(offLinePath).Concat(File.ReadLines(onlinePath));
            List<string> outputList = new List<string>();
            foreach(string s in list)
            {
                string textPath = s.Split('\t')[1];
                string audioPath = s.Split('\t')[2].ToLower();
                Sanity.Requires(File.Exists(textPath));
                Sanity.Requires(File.Exists(audioPath));
                if (!dict.ContainsKey(audioPath))
                    continue;
                string newAudioPath = dict[audioPath];
                string newTextPath = newAudioPath.ToLower().Replace("300hrsrecordingnew", "300hrsannotationnew").Replace(".wav", ".txt");
                string textFolder = GetFolder(newTextPath).Folder;
                Directory.CreateDirectory(textFolder);
                if (File.Exists(newTextPath))
                    Console.WriteLine(textPath);
                else if(!File.Exists(newAudioPath))
                    Console.WriteLine(newAudioPath);
                else
                {
                    File.Copy(textPath, newTextPath);
                    outputList.Add($"{textPath}\t{audioPath}\t{newTextPath}\t{newAudioPath}");
                }
            }
            File.WriteAllLines(outputPath, outputList);
        }
        private (string Folder, string File) GetFolder(string filePath)
        {
            FileInfo file = new FileInfo(filePath);
            return (file.DirectoryName, file.Name);
        }

        #region Match audio id.
        public void MatchAudioId(string matchFilePath, HashSet<int> matchingIds)
        {
            var list = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt")
                .Select(x => new OverallMappingLine(x));
            var mList = File.ReadLines(matchFilePath)
                .Select(x => new AnnotationLine(x));
            var oList = MatchOverall(list, mList, matchingIds).Select(x => x.Output());
            File.WriteAllLines(@"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll_new.txt", oList);
        }
        private IEnumerable<OverallMappingLine> MatchOverall(IEnumerable<OverallMappingLine> overallSequence, IEnumerable<AnnotationLine> annotationSequence, HashSet<int> matchingIds)
        {
            var oGroups = overallSequence.GroupBy(x => x.TaskId);
            var aGroups = annotationSequence.ToLookup(x => x.TaskId);
            foreach(var oGroup in oGroups)
            {
                int key = oGroup.Key == "" ? -1 : int.Parse(oGroup.Key);
                if (matchingIds.Contains(key))
                {
                    Console.WriteLine(oGroup.Key);
                    var newGroup = MatchSingleGroup(oGroup, aGroups[int.Parse(oGroup.Key)]);
                    foreach (var oLine in newGroup)
                        yield return oLine;
                }
                else
                {
                    foreach (var oLine in oGroup)
                        yield return oLine;
                }
            }
        }
        private IEnumerable<OverallMappingLine> MatchSingleGroup(IEnumerable<OverallMappingLine> overallSequence, IEnumerable<AnnotationLine> annotationsequence)
        {
            var oArray = overallSequence.ToArray();
            var aArray = annotationsequence.ToArray();
            Sanity.Requires(oArray.Length == aArray.Length);
            Dictionary<string, AnnotationLine> aDict = aArray.ToDictionary(x => GetRawKey(x.AudioName), x => x);
            
            for(int i = 0; i < oArray.Length; i++)
            {
                string key = GetRawKey(oArray[i].AudioName);
                Sanity.Requires(oArray[i].TaskName == aDict[key].TaskName);
                oArray[i].AudioId = aDict[key].AudioPlatformId.ToString();                
            }
            return oArray;
        }
        #endregion

        #region Word frequency
        public void ExtractTokens()
        {
            string rootPath = @"F:\WorkFolder\Transcripts";
            string outputPath = @"F:\WorkFolder\Summary\20210222\Important\WordFrequency.txt";
            var onlineFolders = Directory.EnumerateDirectories(rootPath, "*online");
            var offlineFolders = Directory.EnumerateDirectories(rootPath, "*offline");
            var list = onlineFolders.Concat(offlineFolders).SelectMany(x => ExtractTokensFromFolder(Path.Combine(x, "output")));
            Dictionary<string, int> freqDict = new Dictionary<string, int>();
            foreach(string s in list)
            {
                if (!freqDict.ContainsKey(s))
                    freqDict[s] = 1;
                else
                    freqDict[s]++;
            }
            var oList = freqDict.OrderBy(x => x.Value).Select(x => $"{x.Key}\t{x.Value}");
            File.WriteAllLines(outputPath, oList);
        }
        private IEnumerable<string> ExtractTokensFromFolder(string folderPath)
        {
            return Directory.EnumerateFiles(folderPath, "*.txt", SearchOption.AllDirectories)
                .SelectMany(x => File.ReadLines(x))
                .SelectMany(x => ExtractTokensFromSentence(x));
        }

        Regex TimeReg = new Regex("\\[.*?\\]", RegexOptions.Compiled);
        Regex TagReg = new Regex("<.*?>", RegexOptions.Compiled);
        static readonly char[] Sep = { ' ' };
        private IEnumerable<string> ExtractTokensFromSentence(string s)
        {
            string rawS = s.Replace("s1", " ")
                .Replace("S1", " ")
                .Replace("s2", " ")
                .Replace("S2", " ");
            rawS = TimeReg.Replace(rawS, " ");
            rawS = TagReg.Replace(rawS, " ");
            return rawS.Split(Sep, StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion
    }
}
