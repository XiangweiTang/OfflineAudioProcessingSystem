using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Common;
using System.Text.RegularExpressions;

namespace OfflineAudioProcessingSystem
{
    class OneOffTasks
    {
        public OneOffTasks()
        { Copy(); }

        public void Copy()
        {
            foreach(string s in File.ReadLines(Constants.TOTAL_MAPPING_PATH))
            {
                var line = new TotalMappingLine(s);
                if (!line.Valid)
                    continue;
                if (!File.Exists(line.DeliveredAudioPath))
                {
                    if (!Directory.Exists(line.DeliveredAudioFolder))
                        Directory.CreateDirectory(line.DeliveredAudioFolder);
                    File.Copy(line.LocalAudioPath, line.DeliveredAudioPath);
                }
            }
        }
    }

    
    class OneOffCutAudios
    {
        public void RunCut()
        {
            //var list = CutAudios();
            string rootPath = @"F:\WorkFolder\300hrsSplit";
            //List<string> o = new List<string>();
            //foreach(var line in list)
            //{
            //    line.CutAudio(rootPath);
            //    line.CutText(rootPath);
            //    o.Add(line.Output());
            //}
            //o.WriteAllLinesToTmp();
            CreateTextFiles(rootPath);
            OverlapList.OrderBy(x=>x).WriteAllLinesToTmp();
        }
        private IEnumerable<CutAudioLine> CutAudios()
        {
            var list = File.ReadLines(Constants.TOTAL_MAPPING_PATH)
                .Select(x => new TotalMappingLine(x));
            foreach(var l in list)
            {
                if (!File.Exists(l.DeliveredTextPath))
                    continue;
                var content = File.ReadLines(l.DeliveredTextPath)
                    .Select(x => LocalCommon.ExtractTransLine(x))
                    .ToArray();
                Sanity.Requires(content.Length%2 == 0);
                for(int i = 0; i < content.Length; i += 2)
                {
                    CutAudioLine line = new CutAudioLine
                    {
                        SourceAudioPath = l.DeliveredAudioPath,
                        SourceTextPath = l.DeliveredTextPath,
                        Dialect = l.Dialect,
                        InputTextPath = l.InputTextPath,
                        SpeakerId = l.UniversalSpeakerId,
                        SessionId=l.UniversalFileId.ToString("00000"),
                        InternalId=(i/2),                 
                        StartTime = content[i].StartTime.ToString(),
                        EndTime = content[i].EndTime.ToString(),
                        SG = content[i].Content,
                        HG = content[i + 1].Content
                    };
                    yield return line;
                }
            }
        }
        List<string> OverlapList = new List<string>();
        private void CreateTextFiles(string outputRootPath)
        {
            var list = File.ReadLines(Constants.TOTAL_MAPPING_PATH)
                .Select(x => new TotalMappingLine(x));
            foreach (var l in list)
            {
                if (!File.Exists(l.DeliveredTextPath))
                    continue;
                var content = File.ReadLines(l.DeliveredTextPath)
                    .Select(x => LocalCommon.ExtractTransLine(x))
                    .ToArray();
                Sanity.Requires(content.Length % 2 == 0);
                string[] arrayTS = new string[content.Length / 2];
                string[] array1 = new string[content.Length / 2];
                string[] array2 = new string[content.Length / 2];
                List<string> textList = new List<string>();
                double preend = 0;
                for (int i = 0; i < content.Length; i += 2)
                {
                    CutAudioLine line = new CutAudioLine
                    {
                        SourceAudioPath = l.DeliveredAudioPath,
                        SourceTextPath = l.DeliveredTextPath,
                        Dialect = l.Dialect,
                        InputTextPath = l.InputTextPath,
                        SpeakerId = l.UniversalSpeakerId,
                        SessionId = l.UniversalFileId.ToString("00000"),
                        InternalId = (i / 2),
                        StartTime = content[i].StartTime.ToString(),
                        EndTime = content[i].EndTime.ToString(),
                        SG = content[i].Content,
                        HG = content[i + 1].Content
                    };
                    textList.Add(string.Join("\t", (i / 2).ToString("00000"), "SG", line.SG));
                    textList.Add(string.Join("\t", (i / 2).ToString("00000"), "HG", line.HG));
                    double start = double.Parse(line.StartTime);
                    if (start - preend<-1)
                    {
                        OverlapList.Add($"{l.Dialect}\t{l.UniversalSpeakerId}\t{l.UniversalFileId:00000}\t{line.StartTime}\t{preend}");
                    }
                    preend = double.Parse(line.EndTime);
                    arrayTS[i / 2] = $"{line.StartTime}\t{line.EndTime}";
                    array1[i / 2] = line.SG;
                    array2[i / 2] = line.HG;
                }
                string folderPath = Path.Combine(outputRootPath, l.Dialect, l.UniversalSpeakerId);
                Directory.CreateDirectory(folderPath);
                string textgridPath = Path.Combine(outputRootPath, l.Dialect, l.UniversalSpeakerId, l.UniversalFileId.ToString("00000") + ".textgrid");
                //TextGrid.TimeStampToTextGrid(arrayTS, textgridPath, array1, array2);
                //string textPath = Path.Combine(outputRootPath, l.Dialect, l.UniversalSpeakerId, l.UniversalFileId.ToString("00000") + ".txt");
                //File.WriteAllLines(textPath, textList);
                //string inputAudioPath = l.DeliveredAudioPath;
                //string newAudioPath= Path.Combine(outputRootPath, l.Dialect, l.UniversalSpeakerId, l.UniversalFileId.ToString("00000") + ".wav");
                //File.Copy(inputAudioPath, newAudioPath);
            }
        }        
    }
    class CutAudioLine : Line
    {
        public string SourceAudioPath { get; set; }
        public string SourceTextPath { get; set; }
        public string Dialect { get; set; }
        public string SpeakerId { get; set; }
        public string SessionId { get; set; }
        public int InternalId { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string SG { get; set; }
        public string HG { get; set; }
        public string InputTextPath { get; set; }
        public CutAudioLine() { }
        public CutAudioLine(string s) : base(s) { }
        protected override IEnumerable<object> GetLine()
        {
            yield return SourceAudioPath;
            yield return SourceTextPath;
            yield return InputTextPath;
            yield return Dialect;
            yield return SpeakerId;
            yield return SessionId;
            yield return InternalId;
            yield return StartTime;
            yield return EndTime;
            yield return SG;
            yield return HG;
        }

        protected override void SetLine(string[] split)
        {
            SourceAudioPath = split[0];
            SourceTextPath = split[1];
            InputTextPath = split[2];
            Dialect = split[3];
            SpeakerId = split[4];
            SessionId = split[5];
            InternalId = int.Parse(split[6]);
            StartTime = split[7];
            EndTime = split[8];
            SG = split[9];
            HG = split[10];
        }

        public void CutAudio(string rootPath)
        {
            string outputFolder = Path.Combine(rootPath, Dialect, SpeakerId,SessionId);
            Directory.CreateDirectory(outputFolder);
            string outputPath = Path.Combine(outputFolder, $"{InternalId:00000}.wav");
            double timeSpan = double.Parse(EndTime) - double.Parse(StartTime);
            LocalCommon.CutAudioWithSox(SourceAudioPath, StartTime, timeSpan, outputPath);
        }
        public void CutText(string rootPath)
        {
            string outputFolder = Path.Combine(rootPath, Dialect, SpeakerId, SessionId);
            Directory.CreateDirectory(outputFolder);
            string outputPath = Path.Combine(outputFolder, $"{InternalId:00000}.txt");
            List<string> list = new List<string>
            {
                "SG",
                SG,
                "HG",
                HG
            };
            File.WriteAllLines(outputPath, list);
        }
    }
    class OneoffMappingFiles
    {
        public OneoffMappingFiles()
        {
            Init();
        }

        private void Init()
        {
            IdTaskDict = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt")
                .Select(x => new OverallMappingLine(x))
                .Select(x => new { x.TaskId, x.TaskName })
                .Distinct().ToDictionary(x => x.TaskId, x => x.TaskName);
            NamePathDict = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\TaskNamePathMapping.txt")
                .ToDictionary(x => x.Split('\t')[0], x => x.Split('\t')[1]);
            SpeakerIdPathDict = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\DailyNamePathMapping.txt")
                .GroupBy(x => x.Split('\t')[0])
                .ToDictionary(x => x.Key, x => x.Select(y => y.Split('\t')[1]).ToHashSet());
        }
        Dictionary<string, string> IdTaskDict = new Dictionary<string, string>();
        Dictionary<string, string> NamePathDict = new Dictionary<string, string>();
        Dictionary<string, HashSet<string>> SpeakerIdPathDict = new Dictionary<string, HashSet<string>>();
        public IEnumerable<(string audioId, string filePath)> GetOnlineFiles()
        {
            return LocalCommon.GetOnlineFolders().SelectMany(x => GetOnlineFiles(x));
        }

        public IEnumerable<(string audioId, string filePath)> GetOnlineFiles(string folderPath)
        {
            string annotatorPath = Path.Combine(folderPath, "FromAnnotation.txt");
            var list = File.ReadLines(annotatorPath)
                .Select(x => new AnnotationLine(x));
            foreach(var line in list)
            {
                string audioFolderPath = Path.Combine(folderPath, "Input", line.TaskId + "_" + IdTaskDict[line.TaskId.ToString()], "Speaker");
                string audioPath = Path.Combine(audioFolderPath, line.AudioName);
                //Sanity.Requires(File.Exists(audioPath));
                yield return (line.AudioPlatformId.ToString(), audioPath);
            }

        }
        Regex Team1Reg = new Regex("^([0-9]{8})_(.*?)_(.*)$", RegexOptions.Compiled);
        Regex Team2Reg = new Regex("^.*?_([0-9_]*)$", RegexOptions.Compiled);

        public IEnumerable<(string, string)> GetOfflineFiles()
        {
            return LocalCommon.GetOfflineFolder().SelectMany(x => GetOfflineFiles(x));
        }
        public IEnumerable<(string,string)> GetOfflineFiles(string folderPath)
        {
            string tgPath = Path.Combine(folderPath, "TextGrid");
            foreach(string taskFolderPath in Directory.EnumerateDirectories(tgPath))
            {
                string taskName = taskFolderPath.Split('\\').Last();
                string sourceFolderPath = "";
                HashSet<string> sourceFolderPaths = new HashSet<string>();
                if (NamePathDict.ContainsKey(taskName))
                {
                     sourceFolderPath= NamePathDict[taskName];
                }
                else if (Team1Reg.IsMatch(taskName))
                {
                    var match = Team1Reg.Match(taskName);
                    sourceFolderPath = Path.Combine(@"F:\WorkFolder\Input\300hrsRecordingContent", match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                }
                else if (Team2Reg.IsMatch(taskName))
                {
                    var match = Team2Reg.Match(taskName);
                    string speakerId = match.Groups[1].Value;
                    sourceFolderPaths = SpeakerIdPathDict[speakerId];
                    if (sourceFolderPaths.Count == 1)
                        sourceFolderPath = sourceFolderPaths.Single();
                }
                else
                {
                    throw new CommonException();
                }
                if (sourceFolderPath != "")
                {
                    Sanity.Requires(Directory.Exists(sourceFolderPath));
                    foreach (var item in OfflinePathMatch(taskFolderPath, sourceFolderPath))
                        yield return item;
                }
                else
                {
                    foreach (var item in OfflinePathMatch(taskFolderPath, sourceFolderPaths))
                        yield return item;
                }
            }
        }

        private IEnumerable<(string, string)> OfflinePathMatch(string offlineFolderPath, string sourceFolderPath)
        {
            foreach(string textGridPath in Directory.EnumerateFiles(offlineFolderPath, "*.textgrid"))
            {
                string wavePath = textGridPath.Split('\\').Last().ToLower().Replace(".textgrid", ".wav");
                string sourceWavePath = Path.Combine(sourceFolderPath, wavePath);
                Sanity.Requires(File.Exists(sourceWavePath));
                yield return (textGridPath, sourceWavePath);
            }
        }

        private IEnumerable<(string,string)> OfflinePathMatch(string offlineFolderPath, HashSet<string> sourceFolderPaths)
        {
            foreach (string textGridPath in Directory.EnumerateFiles(offlineFolderPath, "*.textgrid"))
            {
                string wavePath = textGridPath.Split('\\').Last().ToLower().Replace(".textgrid", ".wav");
                var sourceWavePaths = sourceFolderPaths.Select(x => Path.Combine(x, wavePath));
                string sourceWavePath = sourceWavePaths.Single(x => File.Exists(x));
                yield return (textGridPath, sourceWavePath);
            }
        }
    }

    class OneoffOscarMapping
    {
        private string GetCurrentLine(string filePath, int blockId)
        {
            int index = (blockId - 1) * 2;
            return File.ReadAllLines(filePath)[index];
        }
        private (string deliveredWavPath, string deliveredTxtPath, int blockId, string timeStamp) ExtractOscarMapping(string s, string rootAudioPath, string rootAnnotationPath)
        {
            string id = s.Split(' ')[0];
            var split = id.Split('-');
            string locale = split[1];
            string speakerId = split[2];
            string tail = split[3];
            string audioId = tail.Split('.')[0];
            string bId = tail.Split('.')[1];
            string newWavPath = Path.Combine(rootAudioPath, locale, speakerId, audioId + ".wav");
            string newTxtPath = Path.Combine(rootAnnotationPath, locale, speakerId, audioId + ".txt");
            string tStamp = s.Substring(s.IndexOf(' ') + 1);
            return (newWavPath, newTxtPath, int.Parse(bId), tStamp);
        }
        
        public void Run()
        {
            List<string> oList = new List<string>();
            foreach(string s in File.ReadLines(@"F:\Tmp\ErrorLike.txt"))
            {
                string id = s.Split('\t')[0];
                var r = ExtractOscarMapping(id, @"f:\workfolder\input\300hrsrecordingcontent", @"f:\workfolder\300hrsannotationnew");
                string p = r.deliveredTxtPath.ToLower();
                var line = LocalCommon.DeliveredTxtDict[p];
                string errorLine = GetCurrentLine(line.OldTextPath, r.blockId);
                oList.Add($"{s}\t{line.NewTextPath}\t{line.OldTextPath}\t{errorLine}");
            }
            oList.WriteAllLinesToTmp();
        }
    }

    class OneOffTotalMapping
    {
        public OneOffTotalMapping()
        {
        }
        private void InitAllByLocalPath()
        {
            var dupeUploadAudioIdSet = File.ReadLines(@"F:\WorkFolder\Summary\20210425\DupeUpload.txt").ToHashSet();
            var overallList = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt").Select(x => new OverallMappingLine(x))
                .Where(x => !dupeUploadAudioIdSet.Contains(x.AudioId))
                .ToDictionary(x => x.AudioPath.ToLower(), x => x);

            var g = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\FullMapping.txt")
                .Select(x => new FullMappingLine(x))
                .Where(x => !dupeUploadAudioIdSet.Contains(x.AudioPlatformId.ToString()))
                .GroupBy(x => x.OldPath.ToLower())
                .Where(x => x.Count() > 1)
                .ToArray();

            var fullDict = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\FullMapping.txt")
                .Select(x => new FullMappingLine(x))
                .Where(x => !dupeUploadAudioIdSet.Contains(x.AudioPlatformId.ToString()))
                .ToDictionary(x => x.OldPath.ToLower(), x => x);

            List<string> list = new List<string>();
            foreach(string s in Directory.EnumerateFiles(@"F:\WorkFolder\Input\300hrsRecordingContent", "*.wav", SearchOption.AllDirectories))
            {
                string key = s.ToLower();
                var line1 = overallList.ContainsKey(key) ? overallList[key] : null;
                var line2 = fullDict.ContainsKey(key) ? fullDict[key] : null;
                TotalMappingLine line = new TotalMappingLine
                {
                    TaskName = line1 != null ? line1.TaskName : "",
                    TaskId = line1 != null ?(line1.TaskId==""?int.MinValue: int.Parse(line1.TaskId)) : int.MinValue,
                    AudioName = line1 != null ? line1.AudioName : "",
                    AudioId = line1 != null ?(line1.AudioId==""?int.MinValue: int.Parse(line1.AudioId) ): int.MinValue,
                    Dialect = line1 != null ? line1.Dialect : "",
                    SpeakerId = line1 != null ? line1.Speaker : "",
                    UniversalSpeakerId = line2 != null ? line2.UniversalSpeakerId.ToString("00000") : int.MinValue.ToString(),
                    UniversalFileId = line2 != null ? line2.UniversalAudioId : int.MinValue,
                    InputTextPath = "",
                    InputAudioPath = "",
                    LocalAudioPath = key,
                    Online = line1 != null ? line1.TaskId.Length >= 3 : false,
                    Valid = true,
                };
                list.Add(line.Output());
            }
            list.WriteAllLinesToTmp();
        }

        public void MappingDupe()
        {
            var total = File.ReadAllLines(@"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt")
                .Select(x => new OverallMappingLine(x))
                .GroupBy(x => x.AudioPath.ToLower());
            var dupeDict = File.ReadLines(@"F:\WorkFolder\Summary\20210425\DupeUpload.txt")
                .Select(x => x.Split('\t')[0]).ToDictionary(x => x, x => "");            
            foreach(var g in total)
            {
                var dupes = g.Select(x => x.AudioId);
                string value = dupes.Except(dupeDict.Keys).Single();
                foreach(string dupe in dupes)
                {
                    if (dupe != value)
                        dupeDict[dupe] = value;
                }
            }
            dupeDict.Select(x => $"{x.Key}\t{x.Value}").WriteAllLinesToTmp();
        }

        Dictionary<int, (string wavePath, string textPath)> OnlineMappingDict = new Dictionary<int, (string, string)>();
        Dictionary<string, (string wavePath, string textPath)> OfflineMappingDict = new Dictionary<string, (string wavePath, string textPath)>();
        Dictionary<string, string> TaskNamePathDict = new Dictionary<string, string>();
        Dictionary<string, HashSet<string>> SpeakerPathDict = new Dictionary<string, HashSet<string>>();
        List<string> DupeList = new List<string>();
        private void SetByInput()
        {
            var array = File.ReadLines(@"F:\WorkFolder\Summary\20210425\TotalMapping.txt")
                .Select(x => new TotalMappingLine(x))
                .ToArray();
            TaskNamePathDict = LocalCommon.ReadToDict(@"F:\WorkFolder\Summary\20210425\TaskNamePathMapping.txt");
            SpeakerPathDict = LocalCommon.ReadToMDict(@"F:\WorkFolder\Summary\20210425\DailyNamePathMapping.txt");
            foreach (string onlinePath in Directory.EnumerateDirectories(@"F:\WorkFolder\Transcripts", "*online"))
            {
                SetByInputOnline(onlinePath);
            }
            foreach(string offlinePath in Directory.EnumerateDirectories(@"F:\WorkFolder\Transcripts", "*offline"))
            {
                SetByInputOffline(offlinePath);
            }
            for(int i = 0; i < array.Length; i++)
            {
                array[i].InputAudioPath = "";
                array[i].InputTextPath = "";
            }
            for(int i = 0; i < array.Length; i++)
            {
                if (OnlineMappingDict.ContainsKey(array[i].AudioId))
                {
                    var line = array[i];
                    var r = OnlineMappingDict[array[i].AudioId];
                    line.InputAudioPath = r.wavePath;
                    line.InputTextPath = r.textPath;
                    array[i] = line;
                }
                if (OfflineMappingDict.ContainsKey(array[i].LocalAudioPath))
                {
                    var line = array[i];
                    var r = OfflineMappingDict[array[i].LocalAudioPath];
                    if (line.InputAudioPath != "")
                    {
                        DupeList.Add(line.InputAudioPath);
                        continue;
                    }
                    line.InputAudioPath = r.wavePath;
                    line.InputTextPath = r.textPath;
                    array[i] = line;
                }
            }
            array.Select(x => x.Output()).WriteAllLinesToTmp();
            DupeList.WriteAllLinesToTmp();
        }

        public bool SetByInputOnline(string onlineFolderPath)
        {
            string totalDictPath = @"F:\WorkFolder\Summary\20210425\TotalMapping.txt";
            var totalDict = File.ReadLines(totalDictPath)
                .Select(x => new TotalMappingLine(x))
                .Where(x => x.AudioId > 0)
                .ToDictionary(x => x.AudioId, x => x);
            bool valid = true;
            string annotatorPath = Path.Combine(onlineFolderPath, "FromAnnotation.txt");
            string inputPath = Path.Combine(onlineFolderPath, "Input");
            var list = File.ReadLines(annotatorPath)
                .Select(x => new AnnotationLine(x));
            var inputDict = Directory.EnumerateDirectories(inputPath)
                .ToDictionary(x => x.Split('\\').Last().Split('_')[0], x => Path.Combine(x, "Speaker"));
            foreach (var line in list)
            {
                if (!totalDict.ContainsKey(line.AudioPlatformId))
                {
                    Console.WriteLine(line.AudioPlatformId); 
                }
                if (!inputDict.ContainsKey(line.TaskId.ToString()))
                {
                    valid = false;
                    Console.WriteLine("Abort!\t" + line.TaskId);
                    continue;
                }
                string folderPath = inputDict[line.TaskId.ToString()];
                string wavePath = Path.Combine(folderPath, line.AudioName);
                if (!File.Exists(wavePath))
                {
                    valid = false;
                    Console.WriteLine("Abort!\t" + wavePath);
                    continue;
                }

                string textPath1 = wavePath.Replace(".wav", ".txt");
                string textPath2 = wavePath.Substring(0, wavePath.Length - 3) + "txt";
                string textPath = "";
                if(!File.Exists(textPath1)&&!File.Exists(textPath2))
                {
                    valid = false;
                    Console.WriteLine(textPath1);
                }
                else
                {
                    textPath = File.Exists(textPath1) ? textPath1 : textPath2;
                }
                OnlineMappingDict.Add(line.AudioPlatformId, (wavePath, textPath2));
            }
            return valid;
        }

        public void SetByInputOffline(string offlineFolderPath)
        {
            string inputPath = Path.Combine(offlineFolderPath, "Input");
            foreach(string taskFolder in Directory.EnumerateDirectories(inputPath))
            {
                string speakerFolder = Path.Combine(taskFolder, "Speaker");
                string firstFileName = Directory.EnumerateFiles(speakerFolder, "*.wav").First().Split('\\').Last();
                string sourceFolderPath = GetSourceFolder(taskFolder, firstFileName);
                foreach(string textFilePath in Directory.EnumerateFiles(speakerFolder, "*.txt"))
                {
                    string waveFilePath = textFilePath.Replace(".txt",".wav");
                    if(File.Exists(waveFilePath))
                    {
                        string wavFileName = waveFilePath.Split('\\').Last();
                        string sourcePath = Path.Combine(sourceFolderPath, wavFileName);
                        if(!File.Exists(sourcePath))
                            Console.WriteLine(textFilePath);
                        else
                        {
                            OfflineMappingDict[sourcePath.ToLower()]=(waveFilePath, textFilePath);
                        }
                        continue;
                    }

                    waveFilePath = textFilePath.Substring(0, textFilePath.Length - 3) + "wav";
                    if (File.Exists(waveFilePath))
                    {
                        string wavFileName = waveFilePath.Split('\\').Last();
                        string sourcePath = Path.Combine(sourceFolderPath, wavFileName);
                        if (!File.Exists(sourcePath))
                            Console.WriteLine(textFilePath);
                        else
                            OfflineMappingDict.Add(sourcePath.ToLower(), (waveFilePath, textFilePath));
                        continue;
                    }
                    Console.WriteLine(textFilePath);
                }
            }
        }
        Regex OldNameReg = new Regex("^([0-9]{8})_([a-zA-Z]+)_(.*)$", RegexOptions.Compiled);
        Regex NewNameReg = new Regex("^[a-zA-Z]*_([0-9_]+)$", RegexOptions.Compiled);
        private string GetSourceFolder(string folderPath, string fileName)
        {
            string folderName = folderPath.Split('\\').Last();
            if (TaskNamePathDict.ContainsKey(folderName))
                return TaskNamePathDict[folderName];
            if (OldNameReg.IsMatch(folderName))
            {
                var match = OldNameReg.Match(folderName);
                return Path.Combine(LocalConstants.LOCAL_AUDIO_ROOT_FOLDER, match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
            }
            if (NewNameReg.IsMatch(folderName))
            {
                string speaker = NewNameReg.Match(folderName).Groups[1].Value;
                var candidates = SpeakerPathDict[speaker];
                return candidates.Single(x => File.Exists(Path.Combine(x, fileName)));
            }
            throw new CommonException();
        }
    }

    class OneOffAddSpeakerId
    {
        public void Map()
        {
            var total = File.ReadLines(@"F:\WorkFolder\Summary\20210425\TotalMapping.txt")
                .Select(x => new TotalMappingLine(x)).OrderBy(x => x.LocalAudioPath).ToArray();
            var ids = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\Iddict.txt")
                .Select(x => new IdDictLine(x))
                .ToDictionary(x => x.MergedId, x => x);
            var overall = File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\OverallMappingAll.txt")
                .Select(x => new OverallMappingLine(x)).ToArray();

        }

        private IEnumerable<string> GroupByIdentical(string[] audioPaths)
        {
            List<List<string>> dupes = new List<List<string>>();
            foreach(string audioPath in audioPaths)
            {
                bool set = false;
                foreach(var dupe in dupes)
                {
                    string oldePath = dupe[0];
                    if (LocalCommon.AudioIdenticalLocal(oldePath, audioPath))
                    {
                        dupe.Add(audioPath);
                        set = true;
                        break;
                    }
                }
                if (!set)
                    dupes.Add(new List<string> { audioPath });
            }
            int groupId = 0;
            foreach(var dupe in dupes)
            {
                if (dupe.Count == 1)
                    continue;
                foreach (string s in dupe)
                    yield return $"{groupId}\t{s}";
                groupId++;
            }
        }
    }

    class OneOffSearchInputFiles
    {
        Dictionary<string, string> LocalAudioDict = new Dictionary<string, string>();
        Dictionary<string, TotalMappingLine> TotalDict = new Dictionary<string, TotalMappingLine>();
        public void Run(bool output=false)
        {
            SetDict();
            SearchOnlineFile();
            SearchOfflineFile();
            if (output)
                TotalDict.Values.Select(x => x.Output()).WriteAllLinesToTmp();
        }

        private void SetDict()
        {
            LocalAudioDict = File.ReadLines(Constants.OVERALL_MAPPING_PATH)
                .Select(x => new OverallMappingLine(x))
                .Where(x => x.AudioId.Length == 4)
                .ToDictionary(x => x.AudioId, x => x.AudioPath.ToLower());
            TotalDict = File.ReadLines(Constants.TOTAL_MAPPING_PATH)
                .Select(x => new TotalMappingLine(x))
                .ToDictionary(x => x.LocalAudioPath.ToLower(), x => x);
        }

        public void SearchOnlineFile()
        {
            foreach(string onlinePath in Directory.EnumerateDirectories(@"F:\WorkFolder\Transcripts","*online"))
            {
                SearchOnlineFile(onlinePath);
            }
        }
        public void SearchOfflineFile()
        {
            foreach(string offlinePath in Directory.EnumerateDirectories(@"F:\WorkFolder\Transcripts", "*offline"))
            {
                SearchOfflineFile(offlinePath);
            }
        }
        public void SearchOnlineFile(string onlinePath)
        {
            string inputPath = Path.Combine(onlinePath, "Input");
            Dictionary<string, string> dict = Directory.EnumerateDirectories(inputPath)
                .ToDictionary(x => x.Split('\\').Last().Split('_')[0], x => x);
            string aPath = Path.Combine(onlinePath, "FromAnnotation.txt");
            var list = File.ReadLines(aPath).Select(x => new AnnotationLine(x));
            foreach(var line in list)
            {
                string folder = Path.Combine(dict[line.TaskId.ToString()], "Speaker");
                string textPath = Path.Combine(folder, line.AudioName.Substring(0, line.AudioName.Length - 3) + "txt");
                if (!File.Exists(textPath))
                {
                    Console.WriteLine(textPath);
                    continue;
                }
                string audioId = line.AudioPlatformId.ToString();
                string localAudioPath = LocalAudioDict[audioId];
                if (!File.Exists(localAudioPath))
                {
                    Console.WriteLine(textPath.ToLower());
                    Console.WriteLine(localAudioPath);
                    Console.WriteLine();
                }
                ResetInput(localAudioPath, textPath);
            }
        }
        public void SearchOfflineFile(string offlinePath)
        {
            string inputPath = Path.Combine(offlinePath, "Input");
            string mPath = Path.Combine(offlinePath, "OutputFolderMapping.txt");
            Dictionary<string, string> dict = File.ReadLines(mPath)
                .ToDictionary(x => x.Split('\t')[0].Replace("Output", "Input"), x => x.Split('\t')[1]);
            foreach(string taskPath in Directory.EnumerateDirectories(inputPath))
            {
                string speakerPath = Path.Combine(taskPath, "Speaker");
                string localFolderPath = dict[taskPath];
                foreach(string textPath in Directory.EnumerateFiles(speakerPath,"*.txt"))
                {
                    string textName = textPath.Split('\\').Last();
                    string audioName = textName.Substring(0, textName.Length - 3) + "wav";
                    string localAudioPath = Path.Combine(localFolderPath, audioName);
                    if (!File.Exists(localAudioPath))
                    {
                        Console.WriteLine(textPath.ToLower());
                        Console.WriteLine(localAudioPath);
                        Console.WriteLine();
                        continue;
                    }
                    ResetInput(localAudioPath, textPath);
                }
            }
        }

        private void ResetInput(string localAudioPath, string inputTextPath)
        {
            string path = localAudioPath.ToLower().Replace(@"d:\", @"f:\");
            if (!TotalDict.ContainsKey(path))
            {
                path = path.Replace("20210321", "20210314");
            }
            var line = TotalDict[path];
            if (line.InputTextPath == "")
            {
                TotalDict[path].InputTextPath = inputTextPath.ToLower();
                return;
            }
            if (line.InputTextPath != inputTextPath.ToLower())
            {
                int i = line.InputTextPath.IndexOf("input");
                string iSub = line.InputTextPath.Substring(i);
                int j = inputTextPath.ToLower().IndexOf("input");
                string jSub = inputTextPath.ToLower().Substring(j);
                if (iSub == jSub)
                    return;
                Console.WriteLine(line.InputTextPath);
                Console.WriteLine(inputTextPath);
                Console.WriteLine();
            }  
        }
    }
}
