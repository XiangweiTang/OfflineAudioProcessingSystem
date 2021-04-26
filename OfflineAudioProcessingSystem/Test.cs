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
using OfflineAudioProcessingSystem.TranscriptValidation;

namespace OfflineAudioProcessingSystem
{
    class Test
    {
        Random R = new Random();

        public Test(string[] args)
        {
            //RunTransTest();
            //RunFullMapping();
            //RunTransValidation(@"F:\WorkFolder\Transcripts\20210329_Online", 1.5);
            //UpdateAll(@"F:\WorkFolder\Summary\Update\Update_20210422_ForPre.txt");
            //RunAudioTrans(@"");
            //TestMed();
            //AddDialect(@"F:\WorkFolder\Transcripts\20210321_Online\Input\729_20201230_Luzern_Ruth\Speaker\Luzern Ruth0001 female 64.txt");        
            //RunTest();
        }
        private void TestMed()
        {
            var s = "a b b b".Split(' ');
            var c = "a b b a b".Split(' ');
            MinimumEditDistance<string>.RunWithBackTrack(s, c);
            MinimumEditDistance<string>.BackTrack("").GoThrough();
            var r = MinimumEditDistance<string>.IsUnique;
        }

        private void CompareReUpdate(IEnumerable<string> annotatorUpdates, IEnumerable<string> recheckResult)
        {
            var dict1 = CreateUpdateDict(annotatorUpdates);
            var dict2 = CreateUpdateDict(recheckResult);
            var common = dict1.Keys.Intersect(dict2.Keys);
            foreach(string key in common)
            {
                if (dict1[key] == dict2[key])
                {
                    Console.WriteLine(dict1[key] + "\t" + key);
                }
            }
        }

        private Dictionary<string,string> CreateUpdateDict(IEnumerable<string> seq)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach(string s in seq)
            {
                var split = s.Split('\t');
                if (split[5] == "Too many UNK")
                    continue;
                string key = GetUniversalKey(split);
                string value = split[5];
                dict.Add(key, value);
            }
            return dict;
        }

        private string GetUniversalKey(string[] split)
        {
            return $"{LocalCommon.GetFilePathFromUpdate(split)}|{LocalCommon.GetTransLineKey(LocalCommon.ExtractTransLine(split.Last()))}".ToLower();
        }
        private void UpdateAll(string modifiedPath)
        {
            var groups = File.ReadLines(modifiedPath)
                .Where(x=>x.Split('\t')[5]!= "Too many UNK")
                .GroupBy(x => LocalCommon.GetFilePathFromUpdate(x));
            foreach(var group in groups)
            {
                string filePath = group.Key;
                var list = group.Select(x => LocalCommon.ExtractTransLine(x.Split('\t').Last()));
                LocalCommon.ModifyFileWithUpdate(filePath, list, true);
            }
        }
        
        char[] Sep = { '\\' };
        private void MergeReport()
        {
            List<string> outputList = new List<string>();
            foreach(string offlinePath in Directory.EnumerateDirectories(Constants.ANNOTATION_ROOT_PATH, " * offline"))
            {
                string batchName = offlinePath.Split('\\').Last();
                string allPath = Path.Combine(offlinePath, "All.txt");
                outputList.AddRange(File.ReadLines(allPath).Select(x => $"{batchName}\t{x}"));
            }
            foreach(string onlinePath in Directory.EnumerateDirectories(Constants.ANNOTATION_ROOT_PATH, "*online"))
            {
                string batchName = onlinePath.Split('\\').Last();
                string allPath = Path.Combine(onlinePath, "AllFilter.txt");
                outputList.AddRange(File.ReadLines(allPath).Select(x => $"{batchName}\t{x}"));
            }
            IO.WriteAllLinesToTmp(outputList);
        }
        private void VerifyDF()
        {
            string rootPath = @"F:\WorkFolder\Transcripts";
            foreach(string offlinePath in Directory.EnumerateDirectories(rootPath, "*offline"))
            {
                string mappingPath = Path.Combine(offlinePath, "OutputMapping.txt");
                var list = File.ReadAllLines(mappingPath);
                List<string> outputList = new List<string>();
                foreach(string s in list)
                {
                    string newS = s.Replace("f:\\", "F:\\");
                    outputList.Add(newS);
                }
                Sanity.Requires(list.Length == outputList.Count);
                File.WriteAllLines(mappingPath, outputList);
            }
        }

        private void RunTest()
        {
            /// Add dialect
            //AddDialect(@"F:\WorkFolder\Transcripts\20210329_Online\Input\677_Basel 21.12.2020 Viste\Speaker\Basel-Baseldeutsch3.txt");

            /// Validate trans.
            //RunTransValidation(@"F:\WorkFolder\Transcripts\20210210_Online");
            //RunTransValidationAll();

            /// Update trans.
            //RunTransUpdate(@"F:\WorkFolder\Transcripts\20210329_Offline");
            //RunTransUpdateAll();

            /// Merge text grid.
            //MergeOfflineData(@"F:\WorkFolder\Transcripts\20210329_Offline", true);

            //RunAudioTrans(@"C:\Users\engcheck\Downloads\20210406");

            //GenerateTimeStamp(@"f:\WorkFolder\Input\300hrsRecordingContent\20210406\Visp\10092\");

            //RunTransModifyWithUpdateFile(@"F:\Tmp\online_update.txt");

            //RunTransTest();

            RenameOfflineFolder(@"F:\WorkFolder\Transcripts\20210422_Offline\TextGrid");
        }
        private void RunFullMapping()
        {
            FullMapping fm = new FullMapping();
            fm.GetFilePathFromOscarId(File.ReadLines(@"F:\Tmp\TimestampIssue.txt")).WriteAllLinesToTmp();
            /// Add new reocrding data, this will produce a new file in F:\Tmp folder.
            //fm.AddNewRecordingDataToMappingFile(@"F:\WorkFolder\Input\300hrsRecordingContent\20210310");
            //fm.AddOnlineInfo(@"F:\Tmp\part.txt", @"F:\WorkFolder\Summary\20210222\Important\OnlineStatus.txt");

            /// Create the mapping file for online and offline annotation data.
            //fm.MappingOnlineData(@"F:\WorkFolder\Transcripts\20210321_Online");
            //fm.ProduceOfflineFolderMapping(@"F:\WorkFolder\Transcripts\20210321_Offline");
            //fm.MappingOffLineData(@"F:\WorkFolder\Transcripts\20210321_Offline");
            //fm.MappingOutputDataAll();

            /// Create a file for new recording data.
            //fm.MergeNewRecordingData();
            //fm.CopyNewRecordingData();

            /// Create a file for new annotation data.
            //fm.MergeNewAnnontationData();

            /// Post validation for the delivered data.
            //fm.ValidateAllAnnotationData();

            /// Status for audio and text, create metadata
            //fm.CalculateDeliverAudioInfo();
            //fm.CalculateDeliverTextInfo();
            //fm.CreateMetaData();

            /// Hours for trans and audios.
            //fm.CalculateOutputHours(@"F:\WorkFolder\Transcripts\20210321_Online");
            //fm.CalculateAudioHours(@"F:\WorkFolder\Input\300hrsRecordingContent\20210324", @"F:\WorkFolder\Input\300hrsRecordingContent\20210326", @"F:\WorkFolder\Input\300hrsRecordingContent\20210329");

            /// Create speaker info file.
            //fm.CreateSpeakerInfoFile();

            //fm.OutputRecordingStatusByTeam();
            //fm.OutputRecordingStatusByGenderLocale();

            /// When new audios are coming, add the info.
            //fm.CreateLinesForNewAudios(@"F:\WorkFolder\Input\300hrsRecordingContent\20210324", @"F:\WorkFolder\Input\300hrsRecordingContent\20210326");
        }
        private void RunTransTest()
        {
            TranscriptValidation.TranscriptValidation t = new TranscriptValidation.TranscriptValidation();
            t.Test();            
        }
        
        private void MergeDict()
        {

            var list = File.ReadLines(@"f:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt")
                .Select(x => new OverallMappingLine(x));
            var dictList = File.ReadLines(@"f:\WorkFolder\Summary\20210222\Important\Iddict.txt")
                .Select(x => new IdDictLine(x)).ToArray();
            string outputDictpath = @"f:\WorkFolder\Summary\20210222\Important\Iddict_New.txt";
            var r = dictList.Select(x => x.MergedId)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1).ToArray();
            var dict = dictList.ToDictionary(x => x.MergedId, x => x);
            var validatedDict = dictList.ToDictionary(x => x.UniversalId, x => x);
            HashSet<string> existedKey = new HashSet<string>();
            
            foreach (var line in list)
            {
                if (line.Speaker != "")
                {
                    string mergedKey = $"{line.Dialect}_{line.Speaker}".ToLower();
                    if (!dict.ContainsKey(mergedKey))
                    {
                        dict.Add(mergedKey, new IdDictLine
                        {
                            Age = line.Age,
                            Gender = line.Gender,
                            InternalId = line.Speaker,
                            MergedId = mergedKey,
                        });
                    }
                    else
                    {
                        var value = dict[mergedKey];
                        if (value.Age != line.Age && value.Age != "")
                            Console.WriteLine(mergedKey);
                        if (value.Gender != line.Gender && value.Gender != "")
                            Console.WriteLine(mergedKey);
                    }
                }
            }
            File.WriteAllLines(outputDictpath, dict.Values.Select(x => x.Output()));
        }
        
        private (string folder,string file) GetFolder(string filePath)
        {
            FileInfo file = new FileInfo(filePath);
            return (file.DirectoryName, file.Name);
        }
        private void AddDialect(string[] paths)
        {
            foreach (string path in paths)
                AddDialect(path);
        }
        private void AddDialect(string path)
        {
            const string O_PREFIX = "<chdialects>";
            const string O_SUFFIX = "<chdialects/>";
            const string C_PREFIX = "<chdialects-converted>";
            const string C_SUFFIX = "<chdialects-converted/>";

            Regex OverallRegex = new Regex("^(\\[[0-9. ]+\\])(.*)$", RegexOptions.Compiled);
            var list = File.ReadAllLines(path);
            Sanity.Requires(list.Length % 2 == 0);
            for (int i = 0; i < list.Length; i += 2)
            {
                string o = list[i];
                string c = list[i + 1];
                Sanity.Requires(OverallRegex.IsMatch(o) && OverallRegex.IsMatch(c));
                var oMatch = OverallRegex.Match(o);
                var cMatch = OverallRegex.Match(c);
                list[i] = $"{oMatch.Groups[1].Value} S1 {O_PREFIX}{oMatch.Groups[2].Value} {O_SUFFIX}";
                list[i + 1] = $"{cMatch.Groups[1].Value} S1 {C_PREFIX}{cMatch.Groups[2].Value} {C_SUFFIX}";
            }
            File.Copy(path, path + ".backup");
            File.WriteAllLines(path, list);
        }

        private void GenerateTimeStamp(string folderPath, int cutLevel=3)
        {
            TimeStampTransfer t = new TimeStampTransfer()
            {
                CutLevel = cutLevel
            };
            t.Run(folderPath, folderPath);
            TextGridTransfer tgt = new TextGridTransfer();
            tgt.Run(folderPath, folderPath);
        }

        private IEnumerable<string> GettBottomFolders(string rootPath)
        {
            if (Directory.EnumerateFiles(rootPath, "*.wav").Count() > 0)
            {
                Sanity.Requires(Directory.EnumerateDirectories(rootPath).Count() == 0);
                yield return rootPath;
            }
            else
            {
                foreach (string path in Directory.EnumerateDirectories(rootPath))
                    foreach (string s in GettBottomFolders(path))
                        yield return s;
            }
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

        Regex NumberReg = new Regex("^[0-9_]+$", RegexOptions.Compiled);


        private void DupeCheck(string checkFolder, string outputPath, params string[] oldFolders)
        {
            var newList = Directory.EnumerateFiles(checkFolder, "*.wav", SearchOption.AllDirectories);
            var oldList = oldFolders.SelectMany(x => Directory.EnumerateFiles(x, "*.wav", SearchOption.AllDirectories));
            var list = LocalCommon.AudioIdenticalLocal(newList, oldList);
            File.WriteAllLines(outputPath, list);
        }        
        private void Calculate(string deliverfolder, HashSet<string> exclude)
        {
            List<string> list = new List<string>();
            double d = 0;
            double N = 0;
            foreach (string localePath in Directory.EnumerateDirectories(deliverfolder))
            {
                double totalTime = 0;
                string locale = localePath.Split('\\').Last();
                int n = 0;
                foreach (string wavpath in Directory.EnumerateFiles(localePath, "*.wav"))
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
            string outputRootPath = @"f:\WorkFolder\Delivered";
            string outputFolderPath = Path.Combine(outputRootPath, timeStamp);
            string dupePath = outputFolderPath + ".txt";
            var dict = GetDupeDict();
            DupeCheckTransfer d = new DupeCheckTransfer(dict, dupePath, checkOnly);
            d.Run(inputFolderPath, outputFolderPath);
        }
        private void SetDummy(string i, string o)
        {
            Wave w = new Wave();
            w.ShallowParse(i);
            double l = w.AudioLength;
            int step = 8;
            Random r = new Random();
            List<string> list = new List<string>();
            for (int j = 0; j < l; j += step)
            {
                double start = j + r.NextDouble() * 3;
                double end = start + r.Next(5);
                if (end < l)
                    list.Add($"{start}\t{end}");
            }

            File.WriteAllLines(o, list);
        }

        private void RenameOfflineFolder(string rootFolderPath)
        {
            string path = @"F:\WorkFolder\Summary\20210222\Important\Iddict.txt";
            var dict = File.ReadLines(path)
                .Select(x => new IdDictLine(x))
                .ToDictionary(x => x.UniversalId, x => x);
            foreach (string folderPath in Directory.EnumerateDirectories(rootFolderPath))
            {
                string folderName = folderPath.Split('\\').Last();
                if (dict.ContainsKey(folderName))
                {
                    string newFolderName = dict[folderName].MergedId;
                    string newFolderPath = Path.Combine(rootFolderPath, newFolderName);
                    Directory.Move(folderPath, newFolderPath);
                }
            }
        }
        private void MergeOfflineData(string workFolder, bool useExistingSgHg)
        {
            string textgridFolder = Path.Combine(workFolder, "TextGrid");
            string audioFolder = Path.Combine(workFolder, "Audio");
            string mergeFolder = Path.Combine(workFolder, "Input");
            string reportPath = Path.Combine(workFolder, "TextgridMerge.txt");
            TranscriptValidation.TranscriptValidation t = new TranscriptValidation.TranscriptValidation();
            t.MergeTextGrid(textgridFolder, audioFolder, mergeFolder, reportPath, useExistingSgHg);
        }
        Dictionary<string, string> NameIdDict = new Dictionary<string, string>();
        private void RunAudioTrans(string inputRootFolder)
        {
            RunAudioTransfer rat = new RunAudioTransfer() { InputRootFolder = inputRootFolder };
            rat.Run();
        }

        private void RunTransValidationAll()
        {
            string rootPath = @"F:\WorkFolder\Transcripts";
            var list = Directory.EnumerateDirectories(rootPath, "*offline")
                .Concat(Directory.EnumerateDirectories(rootPath, "*online")).ToArray();
            List<string> totalAllList = new List<string>();
            List<string> totalReportList = new List<string>();
            foreach(string s in list)
            {
                RunTransValidation(s,100);
                totalAllList.AddRange(File.ReadLines(Path.Combine(s, "AllFilter.txt")));
                totalReportList.AddRange(File.ReadLines(Path.Combine(s, "ToAnnotator.txt")));
            }
            File.WriteAllLines(Path.Combine(rootPath, "AllFilter.txt"), totalAllList);
            File.WriteAllLines(Path.Combine(rootPath, "ToAnnotator.txt"), totalReportList);
        }
        private void RunTransValidation(string workFolder, double threshold=1.3, string specificPath = "", bool ignoreDialectTag=false)
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
            string missingPath = Path.Combine(workFolder, "Missing.txt");
            string toAnnnotatorPath = Path.Combine(workFolder, "ToAnnotator.txt");
            string inputAudioTimePath = Path.Combine(workFolder, "AudioTimeInput.txt");
            string inputAudioTimeFilterPath = Path.Combine(workFolder, "AudioTimeInputFilter.txt");
            bool offline = workFolder.ToLower().Split('_').Last() == "offline";
            var diff = Compare(annotatorPath, inputRootPath, offline);
            if (diff.Length > 0)
            {
                foreach (string s in diff)
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
                MissingPath = missingPath,
                InputAudioTimePath=inputAudioTimePath,
                WordCountMismatchThreshold=threshold
            };
            t.RunValidation(specificPath,ignoreDialectTag);
            var set = GetSetFromAnnotator(annotatorPath);
            FilterReport(set, blackListPath, blackListFilterPath);
            FilterReport(set, manuallyPath, manuallFilterPath);
            FilterAll(set, allPath, allFilterPath);
            FilterAudioTime(set, inputAudioTimePath, inputAudioTimeFilterPath);
            if (File.Exists(annotatorPath))
                GenerateAllReport(allFilterPath, reportPath);
            string batchName = workFolder.Split('\\').Last();
            if (File.Exists(annotatorPath))
                ReorgAll(batchName, allFilterPath, toAnnnotatorPath, ReorgAllOnline);
            else
                ReorgAll(batchName, allPath, toAnnnotatorPath, ReorgAllOffline);
        }

        private void RunTransModifyWithUpdateFile(string updateFilePath)
        {
            TranscriptValidation.TranscriptValidation t = new TranscriptValidation.TranscriptValidation();
            t.ModifyWithUpdateFile(updateFilePath);
        }
        private void RunTransUpdateAll()
        {
            string rootPath = @"F:\WorkFolder\Transcripts";
            foreach (string onLinePath in Directory.EnumerateDirectories(rootPath, "*online"))
                RunTransUpdate(onLinePath, true);
            foreach (string offlinePath in Directory.EnumerateDirectories(rootPath, "*offline"))
                RunTransUpdate(offlinePath, false);
        }
        private void RunTransUpdate(string workFolder, bool onLine=true)
        {
            string inputRootPath = Path.Combine(workFolder, "Input");
            string blackListPath = Path.Combine(workFolder, "BlackList.txt");
            string blackListFilterPath = Path.Combine(workFolder, "BlackListFiltered.txt");
            string manuallyPath = Path.Combine(workFolder, "Manually.txt");
            string manuallFilterPath = Path.Combine(workFolder, "ManuallyFiltered.txt");
            string outputRootPath = Path.Combine(workFolder, "Output");
            string mappingPath = Path.Combine(workFolder, "Mapping.txt");
            string annotatorPath = Path.Combine(workFolder, "FromAnnotation.txt");
            string missingPath = Path.Combine(workFolder, "Missing.txt");
            string audioTimePath = Path.Combine(workFolder, "AudioTime.txt");
            foreach (string filePath in Directory.EnumerateFiles(outputRootPath, "*", SearchOption.AllDirectories))
                File.Delete(filePath);
            TranscriptValidation.TranscriptValidation t = new TranscriptValidation.TranscriptValidation
            {
                BlackListPath = blackListPath,
                ManuallyPath = manuallyPath,
                InputRootPath = inputRootPath,
                OutputRootPath = outputRootPath,
                MappingPath = mappingPath,
                MissingPath=missingPath,
                AudioTimePath= audioTimePath
            };
            var set = onLine ? GetSetFromAnnotator(annotatorPath) : null;
            t.RunUpdate(set, true);
        }
        private void FilterReport(Dictionary<string, AnnotationLine> set, string i, string o)
        {
            List<string> list = new List<string>();
            foreach (string s in File.ReadLines(i))
            {
                string filePath = s.Split('\t').Last();
                var split = filePath.Split('\\');
                string id = split[5].Split('_')[0];
                string name = split.Last().Replace(".txt", ".wav");
                string key = $"{id}\t{name}";
                if (set.ContainsKey(key))
                    list.Add(s+"\t"+set[key].Output());
            }
            File.WriteAllLines(o, list);
        }

        private void FilterAll(Dictionary<string, AnnotationLine> set, string i, string o)
        {
            if (set.Count == 0)
            {
                File.Copy(i, o, true);
                return;
            }
            List<string> list = new List<string>();
            foreach (string s in File.ReadLines(i))
            {
                string id = s.Split('\t')[1].Split('_')[0];
                string name = s.Split('\t')[2];
                string key = $"{id}\t{name}.wav";
                if (set.ContainsKey(key))
                    list.Add(s + "\t" + set[key].Output());
            }
            File.WriteAllLines(o, list);
        }

        private void FilterAudioTime(Dictionary<string,AnnotationLine> dict, string i, string o)
        {
            List<string> list = new List<string>();
            var set = dict.Keys.ToHashSet();
            foreach(string s in File.ReadLines(i))
            {
                var r = GetFolder(s.Split('\t')[0]);
                string name = r.file;
                string folder = r.folder.Replace("\\Speaker", "");
                string id = folder.Split('\\').Last().Split('_')[0];
                string key = $"{id}\t{name}";
                if (dict.ContainsKey(key))
                {
                    list.Add(s + "\t" + dict[key].Output());
                    set.Remove(key);
                }
            }
            File.WriteAllLines(o, list);
        }
        private void GenerateAllReport(string i, string o)
        {            
            var list = File.ReadLines(i).Select(x => Reorg(x));
            File.WriteAllLines(o, list);
        }
        private void ReorgAll(string batch, string allFilterPath, string allToAnnotatorPath, Func<string,string,string> f)
        {
            var list = File.ReadLines(allFilterPath)
                .Select(x => f(x, batch));
            File.WriteAllLines(allToAnnotatorPath, list);
        }
        private string ReorgAllOnline(string input, string batch)
        {
            var split = input.Split('\t');
            return string.Join("\t",
                batch,
                split[6],
                split.Length > 9 ? split[9] : "",
                split[5],
                split[8],
                split[3],
                split[4]
                );
        }

        private string ReorgAllOffline(string input, string batch)
        {
            var split = input.Split('\t');
            return string.Join("\t",
                batch,
                split[1],
                split[2],
                "",
                "",
                split[3],
                split[4]
                );
        }
        private string Reorg(string s)
        {
            var split = s.Split('\t');
            string taskId = split[1].Split('_')[0];
            string audioName = split[2];
            string audioId;
            if (Regex.IsMatch(audioName, "^[0-9]{5}$"))
            {
                string taskName = split[1];
                var tSplit = taskName.Split('_');
                string speaker = string.Join("_", tSplit.Skip(3));
                string key = $"{speaker}_{audioName}";
                audioId = NameIdDict[key];
            }
            else
                audioId = NameIdDict[audioName];
            string errorMsg = split[3];
            string xmin = split[4];
            string xmax = split.Length >= 6 ? split[5] : "";
            return $"{taskId}\t{audioId}\t{audioName}\t{errorMsg}\t{xmin}\t{xmax}";
        }
        private void GenerateNameIdDict(string annotatorPath)
        {
            NameIdDict = new Dictionary<string, string>();
            foreach (string s in File.ReadLines(annotatorPath))
            {
                var split = s.Split('\t');
                string audioName = split[4].Split('.')[0];
                if (Regex.IsMatch(audioName, "^[0-9]{5}$"))
                {
                    var nameSplit = split[1].Split('_');
                    string folderName = string.Join("_", nameSplit.Skip(2));
                    string key = $"{folderName}_{audioName}";
                    NameIdDict.Add(key, split[3]);
                }
                else
                    NameIdDict.Add(split[4].Substring(0, split[4].Length - 4), split[3]);
            }
        }
        private string[] Compare(string annotatorpath, string folderPath, bool offline=false)
        {
            var set = GetSetFromAnnotator(annotatorpath);
            var dict = GetFiles(folderPath,offline);
            return set.Keys.Except(dict.Keys).ToArray();
        }

        private Dictionary<string, string> GetFiles(string path, bool offline=false)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string taskIdPath in Directory.EnumerateDirectories(path))
            {
                string id = offline ? taskIdPath.Split('\\').Last() : taskIdPath.Split('\\').Last().Split('_')[0];
                string speakerPath = Path.Combine(taskIdPath, "speaker");
                foreach (string wavPath in Directory.EnumerateFiles(speakerPath, "*.wav"))
                {
                    string fileName = wavPath.Split('\\').Last();
                    string key = $"{id}\t{fileName}";
                    dict.Add(key, wavPath);
                }
            }
            return dict;
        }

        private Dictionary<string, AnnotationLine> GetSetFromAnnotator(string path)
        {
            if (!File.Exists(path))
                return new Dictionary<string, AnnotationLine>();
            return File.ReadLines(path)
                .ToDictionary(x => $"{x.Split('\t')[0]}\t{x.Split('\t')[4]}", x => new AnnotationLine(x));
        }

        private Dictionary<string, HashSet<string>> GetDupeDict()
        {
            string path = @"f:\WorkFolder\Delivered";
            Dictionary<string, HashSet<string>> dict = new Dictionary<string, HashSet<string>>();
            foreach (string filePath in Directory.EnumerateFiles(path, "*.wav", SearchOption.AllDirectories))
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
    class MergeToMapping
    {
        string RootPath = @"f:\WorkFolder\300hrsRecordingNew";
        string NewMappingPath = @"f:\WorkFolder\Summary\20210222\Important\FullMapping.txt";
        string DictPath = @"f:\WorkFolder\Summary\20210222\Iddict.txt";
        string OutputPath = @"f:\WorkFolder\Summary\20210222\Important\FullMapping_New.txt";
        Dictionary<string, int> OldTeamCountDict = new Dictionary<string, int>();
        Dictionary<string, int> PathIdDict = new Dictionary<string, int>();
        List<FullMappingLine> OutList = new List<FullMappingLine>();
        Dictionary<string, IdDictLine> IdDict = new Dictionary<string, IdDictLine>();
        public void RunInit()
        {
            var list = File.ReadLines(NewMappingPath)
                .Select(x => new FullMappingLine(x));
            IdDict = File.ReadLines(DictPath)
                .Select(x => new IdDictLine(x))
                .ToDictionary(x => x.MergedId, x => x);
            foreach (var line in list)
            {
                line.OldPath = line.OldPath.ToLower();
                if (NumReg.IsMatch(line.InternalSpeakerId))
                    SetNewTeamLineUAudioId(line);
                else
                    SetOldTeamLineUAudioId(line);
                string newFolder = Path.Combine(RootPath, line.Locale, line.UniversalSpeakerString);
                Directory.CreateDirectory(newFolder);
                line.NewPath = Path.Combine(newFolder, line.UniversalAudioString + ".wav");
                OutList.Add(line);
            }
            File.WriteAllLines(OutputPath, OutList.Select(x => x.Output()));
        }
        Regex NumReg = new Regex("^[0-9]+$", RegexOptions.Compiled);
        private void SetNewTeamLineUAudioId(FullMappingLine line)
        {
            var split = line.OldPath.Split('\\').Reverse().ToArray();
            line.UniversalAudioId = int.Parse(split[0].Split('.')[0]);
        }
        private void SetOldTeamLineUAudioId(FullMappingLine line)
        {
            line.UniversalSpeakerId = int.Parse(IdDict[line.MergedKey].UniversalId);
            if (PathIdDict.ContainsKey(line.OldPath))
            {
                line.UniversalAudioId = PathIdDict[line.OldPath];
            }
            else
            {
                if (!OldTeamCountDict.ContainsKey(line.MergedKey))
                    OldTeamCountDict[line.MergedKey] = 0;
                OldTeamCountDict[line.MergedKey]++;
                line.UniversalAudioId = OldTeamCountDict[line.MergedKey];
                PathIdDict[line.OldPath] = line.UniversalAudioId;
            }
        }
        public void Run(string newAddPath,bool runCheck=true, bool runCopy=false, bool runMerge=false)
        {
            string dictPath = @"f:\WorkFolder\Summary\20210222\Iddict.txt";
            string oldMappingPath = @"f:\WorkFolder\Summary\20210222\Important\FullMapping.txt";
            if (runCheck)
                RunCheck(newAddPath, dictPath, oldMappingPath, NewMappingPath);
            if (runCopy)
                RunCopy();
            if (runMerge)
                MergeNew(oldMappingPath, NewMappingPath);
        }
        public void Revert()
        {
            var list = File.ReadLines(NewMappingPath)
                .Select(x => new FullMappingLine(x));
            foreach(var line in list)
            {
                if (!string.IsNullOrEmpty(line.NewPath)) 
                
                File.Delete(line.NewPath);
            }
        }
        private void ValidateDict(string dictPath)
        {
            var d1 = File.ReadLines(dictPath).ToDictionary(x => x.Split('\t')[0], x => x);
            var d2 = File.ReadLines(dictPath).ToDictionary(x => x.Split('\t')[1], x => x);
        }
        public void RunCheck(string newAddPath, string dictPath, string oldMappingPath, string newMappingPath)
        {
            var newList = File.ReadLines(newAddPath)
                .Select(x => new NewAddedLine(x)).ToArray();
            ValidateDict(dictPath);
            var idMappingDict = File.ReadLines(dictPath)
                .ToDictionary(x => x.Split('\t')[1], x => x.Split('\t')[0]);
            Sanity.Requires(newList.Select(x => $"{x.Locale}_{x.InternalSpeakerId}").All(x => idMappingDict.ContainsKey(x)));

            var existingList = File.ReadLines(oldMappingPath)
                .Select(x => new FullMappingLine(x)).ToArray();
            HashSet<string> existingPathList = existingList.Select(x => x.OldPath.ToLower()).ToHashSet();
            Dictionary<int, int> currentIdMaxDict = existingList
                .GroupBy(x => x.UniversalSpeakerId)
                .ToDictionary(x => x.Key, x => x.Max(y => y.UniversalAudioId) + 1);
            List<string> outputList = new List<string>();
            foreach(var newLine in newList)
            {
                FullMappingLine line = null;
                if (existingPathList.Contains(newLine.LocalAudioPath.ToLower()))
                {
                    line = new FullMappingLine
                    {
                        AudioPlatformId = newLine.AudioId,
                        OldPath = newLine.LocalAudioPath,
                        Locale = newLine.Locale.ToLower(),
                        InternalSpeakerId = newLine.InternalSpeakerId,
                        UniversalSpeakerId = 99999,
                        NewPath = ""
                    };
                    Console.WriteLine(line.OldPath);
                    outputList.Add(line.Output());
                    continue;
                }
                string key = $"{newLine.Locale}_{newLine.InternalSpeakerId}";
                int universalSpeakerId = int.Parse(idMappingDict[key]);
                if (!currentIdMaxDict.ContainsKey(universalSpeakerId))
                    currentIdMaxDict.Add(universalSpeakerId, 0);
                int universalAudioId = currentIdMaxDict[universalSpeakerId];
                currentIdMaxDict[universalSpeakerId]++;
                string locale = newLine.Locale.ToLower();
                string newFolderPath = Path.Combine(RootPath, locale, universalSpeakerId.ToString("00000"));
                Directory.CreateDirectory(newFolderPath);
                string newPath = Path.Combine(newFolderPath, $"{locale}_{universalSpeakerId:00000}_{universalAudioId:00000}.wav");
                Sanity.Requires(!File.Exists(newPath));
                line = new FullMappingLine
                {
                    AudioPlatformId = newLine.AudioId,
                    OldPath = newLine.LocalAudioPath,
                    Locale = newLine.Locale.ToLower(),
                    InternalSpeakerId = newLine.InternalSpeakerId,
                    UniversalSpeakerId = universalSpeakerId,
                    UniversalAudioId = universalAudioId,
                    NewPath = newPath,
                };
                outputList.Add(line.Output());
            }
            File.WriteAllLines(newMappingPath, outputList);
        }

        public void RunCopy()
        {
            var list = File.ReadLines(NewMappingPath)
                .Select(x => new FullMappingLine(x))
                .Select(x => (x.OldPath, x.NewPath))
                .Distinct();
            foreach (var item in list)
                File.Copy(item.OldPath, item.NewPath);
        }

        private void MergeNew(string oldMappingPath, string newMappingPath)
        {
            var array = File.ReadAllLines(oldMappingPath);
            var list = File.ReadLines(newMappingPath);
            var r = array.Concat(list);
            File.WriteAllLines(oldMappingPath, r);
        }

        public void GenerateReport(string reportPath)
        {
            foreach(string subPath in Directory.EnumerateDirectories(RootPath))
            {
                string locale = subPath.Split('\\').Last();
                
            }
        }
    }
    class DeliverTransfer : FolderTransfer
    {
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            try
            {
                string speakerId = inputPath.Split('\\').Last().Split('_')[1];
                if (int.Parse(speakerId) >= 10000)
                    File.Copy(inputPath, outputPath);
            }
            catch
            {

            }
        }
    }
    class GenerateOnlineDeliverReport
    {
        public string WorkFolder { get; set; }
        string AnnotatorPath;
        string AnnotatorDupePath;
        string OutputPath;
        string OnlineNewPath = @"f:\WorkFolder\Summary\20210222\New_WithSR.txt";
        string OnlineOldPath = @"f:\WorkFolder\Summary\20210222\Old_WithSR.txt";
        Dictionary<string, OverallMappingLine> Dict = new Dictionary<string, OverallMappingLine>();
        public GenerateOnlineDeliverReport(string workFolder)
        {
            WorkFolder = workFolder;
            Init();
            GenerateReport();
        }
        private void Init()
        {
            Dict = File.ReadLines(OnlineNewPath).Concat(File.ReadLines(OnlineOldPath))
                .Select(x => new OverallMappingLine(x))
                .ToDictionary(x => x.AudioId, x => x);
            AnnotatorPath = Path.Combine(WorkFolder, "FromAnnotation.txt");
            AnnotatorDupePath = Path.Combine(WorkFolder, "FromAnnotationDupe.txt");
            OutputPath = Path.Combine(WorkFolder, "Regions.txt");
        }

        private void GenerateReport()
        {
            Dictionary<string, double> dict = new Dictionary<string, double>();
            var list = File.Exists(AnnotatorDupePath)
                ? File.ReadLines(AnnotatorPath).Concat(File.ReadLines(AnnotatorDupePath))
                : File.ReadLines(AnnotatorPath);
            foreach (string s in list)
            {
                string audioId = s.Split('\t')[3];
                var line = Dict[audioId];
                if (!dict.ContainsKey(line.Dialect))
                    dict.Add(line.Dialect, 0);
                Wave w = new Wave();
                w.ShallowParse(line.AudioPath);
                dict[line.Dialect] += w.AudioLength;
            }

            File.WriteAllLines(OutputPath, dict.Select(x => x.Key + "\t" + x.Value / 3600));
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
        public DupeCheckTransfer(Dictionary<string, HashSet<string>> dict, string dupePath, bool checkOnly) : base()
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
                foreach (string existingPath in Dict[key])
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
        public int CutLevel { get; set; } = 3;
        protected override IEnumerable<string> GetItems(string inputFolderPath)
        {
            return Directory.EnumerateFiles(inputFolderPath, "*.wav");
        }
        public override string ItemRename(string inputItemName)
        {
            FileInfo file = new FileInfo(inputItemName);
            return inputItemName.Substring(0, inputItemName.Length - 4) + ".txt";
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            if (!File.Exists(outputPath))
                LocalCommon.SetTimeStampsWithVad(inputPath, outputPath, CutLevel);
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
        string reportRootFolder = @"f:\Tmp\_Report";
        string TimeStamp = DateTime.Now.ToString("yyyyMMdd_hhmmss");
        public string InputRootFolder { get; set; } = null;
        string workRootFolder { get; set; } = @"f:\WorkFolder\OverallTmp";
        string dailyRootFolder = @"f:\WorkFolder\DailyFolder";
        string existingPath = @"f:\WorkFolder\Input\Summary.txt";
        string outputRootFolder = @"f:\WorkFolder\Input\300hrsRecordingContent";
        public void Run()
        {
            string inputTimeStamp = InputRootFolder.Split('\\').Last();
            List<string> overallReportList = new List<string>();
            string overallReportPath = Path.Combine(reportRootFolder, TimeStamp + ".report.txt");            
            List<string> overallErrorList = new List<string>();
            string overallErrorPath = Path.Combine(reportRootFolder, TimeStamp + ".error.txt");
            foreach (string localeFolder in Directory.EnumerateDirectories(InputRootFolder))
            {
                string locale = localeFolder.Split('\\').Last();
                foreach (string speakerIdFolder in Directory.EnumerateDirectories(localeFolder))
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
                    overallReportList.AddRange(File.ReadLines(reportPath));
                    overallErrorList.AddRange(File.ReadLines(errorPath));

                    string dailyFolder = Path.Combine(dailyRootFolder, TimeStamp, $"{inputTimeStamp}_{locale}_{speakerId}");
                    Directory.CreateDirectory(dailyFolder);
                    FolderCopy fc = new FolderCopy();
                    fc.Run(outputFolder, dailyFolder);
                }
            }
            File.WriteAllLines(overallReportPath, overallReportList);
            File.WriteAllLines(overallErrorPath, overallErrorList);
        }
    }

    class MapTranscript
    {
        public void MapOnlineFile(string workFolder)
        {
            string annotatorPath = Path.Combine(workFolder, "FromAnnotation.txt");
            string overallMappingPath = @"f:\WorkFolder\Summary\20210222\Important\FullMapping.txt";
            string transMappingPath = Path.Combine(workFolder, "Mapping.txt");
            var annotatorList = File.ReadLines(annotatorPath)
                .Select(x => new AnnotationLine(x))
                .ToDictionary(x => x.AudioPlatformId, x => x);
            var overallMappingDict = File.ReadLines(overallMappingPath)
                .Select(x => new FullMappingLine(x))
                .ToDictionary(x => x.AudioPlatformId, x => x);
            List<string> outputList = new List<string>();
            foreach(int key in annotatorList.Keys)
            {
                if (overallMappingDict.ContainsKey(key))
                {
                    string transPath = "";
                    var annotatorLine = annotatorList[key];
                    var overallMappingLine = overallMappingDict[key];
                    string folderPath = Directory.EnumerateDirectories(Path.Combine(workFolder, "output"))
                        .Single(x => annotatorLine.TaskId == int.Parse(x.Split('\\').Last().Split('_')[0]));
                    string filePath = Path.Combine(folderPath,"speaker", annotatorLine.AudioName.Replace(".wav", ".txt"));
                    if (File.Exists(filePath))
                    {
                        transPath = filePath;
                    }
                    TransMappingLine line = new TransMappingLine
                    {
                        OnlineId = key.ToString(),
                        Locale=overallMappingLine.Locale,
                        UniversalSpeakerString = overallMappingLine.UniversalSpeakerString,
                        UniversalAudioString = overallMappingLine.UniversalAudioString,
                        OldAudioPath = overallMappingLine.OldPath,
                        OldTransPath = transPath,
                    };
                    outputList.Add(line.Output());
                }
            }
            File.WriteAllLines(transMappingPath, outputList);
        }
        public void CopyTransFile(string workFolder)
        {
            string outputRootFolder = @"f:\WorkFolder\300hrsAnnotationNew";
            string mappingPath= Path.Combine(workFolder, "Mapping.txt");
            var list = File.ReadLines(mappingPath)
                .Select(x => new TransMappingLine(x));
            foreach(var line in list)
            {
                string folderPath = Path.Combine(outputRootFolder, line.Locale, line.UniversalSpeakerString);
                Directory.CreateDirectory(folderPath);
                string filePath = Path.Combine(folderPath, $"{line.UniversalAudioString}.txt");
                if (!string.IsNullOrWhiteSpace(line.OldTransPath))
                {
                    if(!File.Exists(filePath))
                    File.Copy(line.OldTransPath, filePath);
                    else
                        Console.WriteLine(filePath);
                }
            }
        }
        
    }

    class ApplyUpdate
    {
        public void SetKeyForAll(string inputPath)
        {
            var list = File.ReadLines(inputPath)
                .Select(x => Transform(x));
            IO.WriteAllLinesToTmp(list);
        }

        private string Transform(string s)
        {
            var split = s.Split('\t');
            string taskName = split[1];
            string audioName = split[2];
            string fullContent = split[4];
            string timeStamp = fullContent.Split(']')[0].Trim('[');
            string tag = Regex.Match(fullContent, "<.*?>").Groups[0].Value;
            return $"{taskName}|{audioName}|[{timeStamp}]|{tag}\t{split[0]}";
        }
    }

    class RunReport
    {
        public void CreateReport()
        {
            string audioRootPath = @"f:\WorkFolder\300hrsRecordingNew";
            string transRootPath = @"f:\WorkFolder\300hrsAnnotationNew";
            string audioReportPath = Path.Combine(audioRootPath, "Report.txt");
            string transReportPath = Path.Combine(transRootPath, "Report.txt");
            Dictionary<string, double> audioDict = new Dictionary<string, double>();
            Dictionary<string, double> transDict = new Dictionary<string, double>();
            foreach(string localePath in Directory.EnumerateDirectories(audioRootPath))
            {
                string locale = localePath.Split('\\').Last();
                audioDict.Add(locale, 0);
                transDict.Add(locale, 0);
                foreach(string speakerPath in Directory.EnumerateDirectories(localePath))
                {
                    string speaker = speakerPath.Split('\\').Last();
                    foreach(string audioPath in Directory.EnumerateFiles(speakerPath))
                    {
                        string fileName = audioPath.Split('\\').Last();
                        string transPath = Path.Combine(transRootPath, locale, speaker, fileName.Replace(".wav", ".txt"));
                        Wave w = new Wave();
                        w.ShallowParse(audioPath);
                        audioDict[locale] += w.AudioLength;
                        if (File.Exists(transPath))
                        {
                            transDict[locale] += w.AudioLength;
                        }
                    }
                }
            }
            File.WriteAllLines(audioReportPath, GetReport(audioDict));
            File.WriteAllLines(transReportPath, GetReport(transDict));
        }

        private IEnumerable<string> GetReport(Dictionary<string,double> dict)
        {
            double d = 0;
            foreach(var item in dict)
            {
                yield return $"{item.Key}\t{item.Value}\t{item.Value / 3600}";
                d += item.Value;
            }
            yield return $"Total\t{d}\t{d / 3600}";
        }
    }
    class TransMappingLine : Line
    {
        public TransMappingLine(string s) : base(s) { }
        public TransMappingLine() : base() { }
        public string OnlineId { get; set; }
        public string Locale { get; set; }
        public string UniversalSpeakerString { get; set; }
        public string UniversalAudioString { get; set; }
        public string OldAudioPath { get; set; }
        public string OldTransPath { get; set; }
        protected override IEnumerable<object> GetLine()
        {
            yield return OnlineId;
            yield return Locale;
            yield return UniversalSpeakerString;
            yield return UniversalAudioString;
            yield return OldAudioPath;
            yield return OldTransPath;
        }

        protected override void SetLine(string[] split)
        {
            OnlineId = split[0];
            Locale = split[1];
            UniversalSpeakerString = split[2];
            UniversalAudioString = split[3];
            OldAudioPath = split[4];
            OldTransPath = split[5];
        }
    }
}
