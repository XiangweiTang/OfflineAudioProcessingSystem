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
        Regex NumberReg = new Regex("^[0-9_]+$", RegexOptions.Compiled);
        public void OutputRecordingStatusByGenderLocale()
        {
            var list = File.ReadLines(Constants.OVERALL_MAPPING_PATH)
                .Select(x => new OverallMappingLine(x));
            var dict = File.ReadLines(Constants.SPEAKER_DICT_PATH)
                .Select(x => new IdDictLine(x))
                .ToDictionary(x => x.MergedId, x => x);
            HashSet<string> existingPathSet = new HashSet<string>();
            Dictionary<string, double> statusDict = new Dictionary<string, double>();
            foreach(var line in list)
            {
                string audioPath = line.AudioPath.ToLower();
                if (existingPathSet.Contains(audioPath))
                    continue;
                existingPathSet.Add(audioPath);
                string key;
                if (!dict.ContainsKey(line.MergedId))
                    key = $"{line.Dialect}_{line.Gender}";
                else
                    key = $"{line.Dialect}_{dict[line.MergedId].Gender}";
                if (!statusDict.ContainsKey(key))
                    statusDict[key] = 0;
                statusDict[key] += double.Parse(line.AudioTime);
            }
            var outputList = statusDict.OrderBy(x => x.Key).Select(x => $"{x.Key}\t{x.Value}");
            File.WriteAllLines(GetCurrentTmpFile(), outputList);
        }
        public void OutputRecordingStatusByTeam()
        {
            string inputPath = @"F:\WorkFolder\Input\300hrsRecordingContent";
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
            List<string> outputList = new List<string>();
            foreach (var item in dict.OrderBy(x => x.Key))
            {
                outputList.Add($"{item.Key}\t{item.Value}\t{item.Value / 3600}");
                //Console.WriteLine($"{item.Key}\t{item.Value}\t{item.Value / 3600}");
                d += item.Value;
            }
            outputList.Add($"All\t{d}\t{d / 3600}");
            File.WriteAllLines(GetCurrentTmpFile(), outputList);
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

        public void CreateMetaData()
        {
            string wavMetaDataPath = @"f:\WorkFolder\300hrsRecordingNew\Recording.metadata.txt";
            string textMetaDataPath = @"f:\WorkFolder\300hrsAnnotationNew\Annotation.metadata.txt";
            string mergedPath = @"f:\WorkFolder\Merged.metadata.txt";
            CreateMetaData(@"f:\WorkFolder\300hrsRecordingNew", "*.wav", wavMetaDataPath);
            CreateMetaData(@"f:\WorkFolder\300hrsAnnotationNew", "*.txt", textMetaDataPath);
            MergeMetaData(wavMetaDataPath, textMetaDataPath, mergedPath);
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
                            FileId = fileName.Split('.')[0],
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
        private void MergeMetaData(string wavMetaDataPath, string textMetaDataPath, string outputPath)
        {
            Dictionary<string, MetaDataLine> metaDict = File.ReadLines(textMetaDataPath)
                .Select(x => new MetaDataLine(x))
                .ToDictionary(x => GetMetaDataKey(x), x => x);
            var list = File.ReadLines(wavMetaDataPath)
                .Select(x => new MetaDataLine(x));
            List<string> outputList = new List<string>();
            foreach(var line in list)
            {
                string key = GetMetaDataKey(line);
                string value = metaDict.ContainsKey(key) ? metaDict[key].RelativePath : "";
                string s = string.Join("\t",
                    line.Locale,
                    line.SpeakerId,
                    line.FileId,
                    line.RelativePath,
                    value,
                    line.RecordedBy,
                    line.AnnotatedBy
                    );
                outputList.Add(s);
            }
            File.WriteAllLines(outputPath, outputList);
        }

        private string GetMetaDataKey(MetaDataLine line)
        {
            return $"{line.Locale}\t{line.SpeakerId}\t{line.FileId}";
        }
        public void CreateSpeakerInfoFile()
        {
            string inputPath = @"f:\WorkFolder\Summary\20210222\Important\Iddict.txt";
            string outputPath = @"f:\WorkFolder\Summary\20210222\Important\SpeakerInfo.txt";
            string uploadPath = @"F:\WorkFolder\SpeakerInfo.txt";
            var list = File.ReadLines(inputPath)
                .Select(x => new IdDictLine(x))
                .Select(x => $"{x.UniversalId}\t{x.MergedId.Split('_')[0]}\t{x.Gender}\t{x.Age}");
            File.WriteAllLines(outputPath, list);
            File.Copy(outputPath, uploadPath, true);
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

        #region Output data mapping
        public void MappingOutputDataAll()
        {
            string rootPath = @"F:\WorkFolder\Transcripts";
            MappingOnlineDataAll(rootPath);
            MappingOfflineDataAll(rootPath);
        }
        public void MappingOnlineDataAll(string rootPath)
        {
            List<string> list = new List<string>();
            foreach(string onlineFolder in Directory.EnumerateDirectories(rootPath, "*online"))
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
        private Dictionary<string, string> RawMapping(string folderPath)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string audioPath in Directory.EnumerateFiles(folderPath, "*.wav"))
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
            foreach (char c in s.ToLower())
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
            foreach (string offlineFolder in Directory.EnumerateDirectories(rootPath, "*offline"))
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
            foreach (string taskFolder in Directory.EnumerateDirectories(outputFolderPath))
            {
                string realFolder = folderMappingDict[taskFolder];
                var realDict = RawMapping(realFolder);
                string speakerFolder = Path.Combine(taskFolder, "Speaker");
                foreach (string audioPath in Directory.EnumerateFiles(speakerFolder, "*.wav"))
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
        #endregion

        
        public void MergeNewRecordingData()
        {
            string oldFilesMappingPath = @"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt";
            string oldNewMappingPath = @"F:\WorkFolder\Summary\20210222\Important\FullMapping.txt";
            // Full mapping lines, old file paths only.
            var oldList = File.ReadLines(oldFilesMappingPath)
                .Select(x => new OverallMappingLine(x)).ToArray();
            // The mapping between old path to new path.
            var oldNewMappingList = File.ReadLines(oldNewMappingPath)
                .Select(x => new FullMappingLine(x)).ToArray();
            HashSet<string> existingFileSet = oldNewMappingList.Select(x => x.OldPath).ToHashSet();

            List<string> outputList = new List<string>();
            string outputPath = @"F:\WorkFolder\Summary\20210222\Important\FullMapping_New.txt";
            string idDictPath = @"F:\WorkFolder\Summary\20210222\Important\Iddict.txt";
            string archiveRootPath = @"F:\WorkFolder\300hrsRecordingNew";
            // The dictionary, key is the merged id, value is the dict line.
            var dictMerged = File.ReadLines(idDictPath)
                .Select(x => new IdDictLine(x))
                .ToDictionary(x => x.MergedId, x => x);
            // To verify the universal id is unique.
            var dictValidate = File.ReadLines(idDictPath)
                .Select(x => new IdDictLine(x))
                .ToDictionary(x => x.UniversalId, x => x);
            // Existing id set.
            var existingIdSet = oldNewMappingList.Select(x => x.MergedKey).ToHashSet();
            // Existing id dictionary, value is the current count.
            Dictionary<string, int> existingCountDict = new Dictionary<string, int>();
            foreach (var line in oldNewMappingList)
            {
                // Get the max id of the existing file.
                if (!existingCountDict.ContainsKey(line.MergedKey))
                    existingCountDict.Add(line.MergedKey, line.UniversalAudioId);
                else if (existingCountDict[line.MergedKey] < line.UniversalAudioId)
                    existingCountDict[line.MergedKey] = line.UniversalAudioId;
            }
            double d = 0;

            // Merge data. 
            foreach (var line in oldList)
            {
                // If the file exists already, or no speaker id here, then skip.
                if (existingFileSet.Contains(line.AudioPath.ToLower()))
                    continue;
                if (line.Speaker == "")
                    continue;
                // For now skip the data with same speaker id(mergedkey).
                if (existingIdSet.Contains(line.MergedId))
                {
                    Console.WriteLine(line.AudioPath);
                    continue;
                }
                d += double.Parse(line.AudioTime);
                string mergedKey = line.MergedId;
                int uSpeakerId = int.Parse(dictMerged[mergedKey].UniversalId);
                if (!existingCountDict.ContainsKey(mergedKey))
                    existingCountDict.Add(mergedKey, 0);
                else existingCountDict[mergedKey]++;
                int uAudioId = existingCountDict[mergedKey];
                FullMappingLine mLine = new FullMappingLine
                {
                    Age = line.Age == "" ? 0 : int.Parse(line.Age),
                    AudioPlatformId = line.AudioId == "" ? 0 : int.Parse(line.AudioId),
                    Gender = line.Gender,
                    InternalSpeakerId = line.Speaker,
                    Locale = line.Dialect,
                    OldPath = line.AudioPath.ToLower(),
                    UniversalSpeakerId = uSpeakerId,
                    UniversalAudioId = uAudioId,
                    NewPath = Path.Combine(archiveRootPath, line.Dialect, uSpeakerId.ToString("00000"), $"{uAudioId:00000}.wav").ToLower()
                };
                if(File.Exists(mLine.NewPath))
                    Console.WriteLine($"Conflict!\t{mLine.OldPath}");
                outputList.Add(mLine.Output());
            }
            Console.WriteLine(d / 3600);
            File.WriteAllLines(outputPath, outputList);
        }
        public void CopyNewRecordingData()
        {
            string newPath = @"F:\WorkFolder\Summary\20210222\Important\FullMapping_New.txt";
            foreach(var line in File.ReadLines(newPath).Select(x=>new FullMappingLine(x)))
            {
                string folderPath = GetFolder(line.NewPath).Folder.ToLower();
                Directory.CreateDirectory(folderPath);
                File.Copy(line.OldPath, line.NewPath);
            }
        }
        public void MergeNewAnnontationData()
        {
            string offLinePath = @"F:\WorkFolder\Transcripts\OfflineMapping.txt";
            string onlinePath = @"F:\WorkFolder\Transcripts\OnlineMapping.txt";
            string overallMappingPath = @"F:\WorkFolder\Summary\20210222\Important\FullMapping.txt";
            string oldMappingPath= @"F:\WorkFolder\Summary\20210222\Important\TransMapping.txt";
            string outputPath = @"F:\WorkFolder\Summary\20210222\Important\TransMapping_New.txt";
            var existingList = File.ReadLines(oldMappingPath).Select(x=>x.ToLower()).ToHashSet();
            var dict = File.ReadLines(overallMappingPath)
                .Select(x => new FullMappingLine(x))
                .Select(x => new { x.OldPath, x.NewPath })
                .Distinct()
                .ToDictionary(x => x.OldPath.ToLower(),x=>x.NewPath);

            var list = File.ReadLines(offLinePath).Concat(File.ReadLines(onlinePath));
            List<string> outputList = new List<string>();
            foreach(string s in list)
            {
                string textPath = s.Split('\t')[1].ToLower();
                string audioPath = s.Split('\t')[2].ToLower();
                Sanity.Requires(File.Exists(textPath));
                Sanity.Requires(File.Exists(audioPath));
                if (!dict.ContainsKey(audioPath))
                    continue;
                string newAudioPath = dict[audioPath];
                string newTextPath = newAudioPath.ToLower().Replace("300hrsrecordingnew", "300hrsannotationnew").Replace(".wav", ".txt");
                string newS = $"{textPath}\t{audioPath}\t{newTextPath}\t{newAudioPath}";
                if (existingList.Contains(newS))
                    continue;
                string textFolder = GetFolder(newTextPath).Folder;
                Directory.CreateDirectory(textFolder);
                if (File.Exists(newTextPath))
                    Console.WriteLine(textPath);
                else if(!File.Exists(newAudioPath))
                    Console.WriteLine(newAudioPath);
                else
                {
                    //File.Copy(textPath, newTextPath);
                    outputList.Add(newS);
                }
            }
            File.WriteAllLines(outputPath, outputList);
        }
        private (string Folder, string File) GetFolder(string filePath)
        {
            FileInfo file = new FileInfo(filePath);
            return (file.DirectoryName, file.Name);
        }

        public void AddNewRecordingDataToMappingFile(string newAudioPath)
        {
            string outputRootPath = Path.Combine(@"F:\Tmp", $"{DateTime.Now:yyyyMMddhhmmss}.txt");
            var list = Directory.EnumerateFiles(newAudioPath, "*.wav", SearchOption.AllDirectories)
                .Select(x => CreateLine(x).Output());
            File.WriteAllLines(outputRootPath, list);
        }
        private OverallMappingLine CreateLine(string filePath)
        {
            var r = GetFolder(filePath);
            Wave w = new Wave();
            w.ShallowParse(filePath);
            var split = filePath.Split('\\').Reverse().ToArray();
            return new OverallMappingLine
            {
                AudioFolder = r.Item1,
                AudioName = r.Item2,
                AudioPath = filePath,
                AudioTime = w.AudioLength.ToString(),
                Age = "",
                Speaker = split[1],
                //AudioId=split[0].Split('.')[0],
                Dialect=split[2],
            };
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

        #region PostCheckAnnotationData
        Regex ValidLineReg = new Regex("^(\\[[0-9.]+ [0-9.]+]) S[12] <([a-z\\-]+)>(.*)<([a-z\\-]+)\\/>$", RegexOptions.Compiled);
        public void ValidateAllAnnotationData()
        {
            string rootPath = @"F:\WorkFolder\300hrsAnnotationNew";
            foreach (string localeDirectory in Directory.EnumerateDirectories(rootPath))
            {
                foreach (string filePath in Directory.EnumerateFiles(localeDirectory, "*.txt", SearchOption.AllDirectories))
                {
                    ValidateSingleFile(filePath);
                }
            }
        }
        private void ValidateSingleFile(string filePath)
        {
            var list = File.ReadAllLines(filePath);
            Sanity.Requires(list.Length % 2 == 0, "Line count is not even.");
            bool trimLastInterval = false;
            for(int i = 0; i < list.Length; i += 2)
            {
                string s1 = list[i];
                string s2 = list[i + 1];
                try
                {
                    ValidateSentencePair(s1, s2, i, list.Length);
                }
                catch(CommonException e)
                {
                    Console.WriteLine(filePath + "\t" + e.Message);
                    if (e.HResult == 1)
                        trimLastInterval = true;
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Extra error:\t{filePath}\t{e.Message}");
                }
            }
            if (trimLastInterval)
                File.WriteAllLines(filePath, list.Take(list.Length - 2));
        }
        private void ValidateSentencePair(string s1, string s2, int i, int n)
        {
            Sanity.Requires(ValidLineReg.IsMatch(s1), "Line format error.");
            Sanity.Requires(ValidLineReg.IsMatch(s2), "Line format error.");
            var match1 = ValidLineReg.Match(s1);
            var match2 = ValidLineReg.Match(s2);

            string prefix1 = match1.Groups[2].Value;
            string suffix1 = match1.Groups[4].Value;
            Sanity.Requires(prefix1 == "chdialects", "Tag error.");
            Sanity.Requires(prefix1 == suffix1, "Tag mismatch.");

            string prefix2 = match2.Groups[2].Value;
            string suffix2 = match2.Groups[4].Value;
            Sanity.Requires(prefix2 == "chdialects-converted", "Tag error.");
            Sanity.Requires(prefix2 == suffix2, "Tag mismatch");

            string timeStamp1 = match1.Groups[1].Value;
            string timeStamp2 = match2.Groups[1].Value;
            Sanity.Requires(timeStamp1 == timeStamp2, $"Time stamp mismatch. {timeStamp1}");
            double xmin1 = double.Parse(timeStamp1.Split(' ')[0].TrimStart('['));
            double xmax1 = double.Parse(timeStamp1.Split(' ')[1].TrimEnd(']'));
            if (i == n - 2)
                Sanity.Requires(xmax1 > xmin1, $"Time stamp error, in the end. {xmin1}", 1);
            else
                Sanity.Requires(xmax1 > xmin1, $"Time stamp error. {xmin1}");

            string content1 = match1.Groups[3].Value;
            string content2 = match2.Groups[3].Value;
            Sanity.Requires(!string.IsNullOrWhiteSpace(content1) && !string.IsNullOrWhiteSpace(content2), $"Empty content. {timeStamp1}");
        }
        #endregion


        public void CalculateOutputHours(string workFolder)
        {
            string mappingPath = Path.Combine(workFolder, "OutputMapping.txt");
            string outputPath = Path.Combine(workFolder, "Report.txt");
            string fullPath = @"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt";
            var dict = File.ReadLines(fullPath)
                .Select(x => new OverallMappingLine(x))
                .Select(x => new { x.AudioPath, x.Dialect, x.AudioTime })
                .Distinct()
                .ToDictionary(x => x.AudioPath.ToLower(), x => x);

            var list = File.ReadLines(mappingPath);
            var audioTimeDict = new Dictionary<string, double>();
            foreach (string s in list)
            {
                var split = s.Split('\t');
                string oldPath = split[2].ToLower();
                var value = dict[oldPath];
                if (!audioTimeDict.ContainsKey(value.Dialect))
                    audioTimeDict[value.Dialect] = 0;
                audioTimeDict[value.Dialect] += double.Parse(value.AudioTime);

            }
            var outputList = audioTimeDict.Select(x => $"{ x.Key}\t{x.Value:0.00}\t{x.Value / 3600:0.00}");
            File.WriteAllLines(outputPath, outputList);
        }

        public void CalculateAudioHours(params string[] folders)
        {
            Dictionary<string, double> dict = new Dictionary<string, double>();
            foreach(string folder in folders)
            {
                foreach(string localePath in Directory.EnumerateDirectories(folder))
                {
                    string locale = localePath.Split('\\').Last();
                    if (!dict.ContainsKey(locale))
                        dict.Add(locale, 0);
                    dict[locale] += GetFolderHours(localePath);
                }
            }
            var list = dict.Select(x => $"{x.Key}\t{x.Value}\t{x.Value / 3600:0.00}");
            File.WriteAllLines(GetCurrentTmpFile(), list);
        }

        private string GetCurrentTmpFile()
        {
            return Path.Combine(@"f:\tmp", $"{DateTime.Now:yyyyMMddhhmmss}.txt");
        }
    }
}
