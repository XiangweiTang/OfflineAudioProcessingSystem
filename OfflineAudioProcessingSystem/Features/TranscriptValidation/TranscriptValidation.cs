using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;
using System.Text.RegularExpressions;

namespace OfflineAudioProcessingSystem.TranscriptValidation
{
    class TranscriptValidation
    {
        public string BlackListPath { get; set; } = @"BlackList.txt";
        public string ManuallyPath { get; set; } = @"Manually.txt";
        public string AllPath { get; set; } = "All.txt";
        public string InputRootPath { get; set; } = @"";
        public string OutputRootPath { get; set; } = @"";
        public string MappingPath { get; set; } = @"";
        const int MAX_SHOW_COUNT = 50;
        List<string> FullList = new List<string>();
        public TranscriptValidation()
        {
        }
        public void Test()
        {
            
        }
        public void RunValidation()
        {
            Init();
            ValidateFolder(InputRootPath);            
            File.WriteAllLines(AllPath, FullList);
        }
        public void RunUpdate(HashSet<string> set, bool create = false)
        {
            Init();
            UpdateFolder(InputRootPath, OutputRootPath, set, create);
        }
        HashSet<string> TagSet = new HashSet<string>();
        (string, string)[] ReplaceArray = new (string, string)[0];
        HashSet<string> ValidTagSet = new HashSet<string>();
        HashSet<char> CharSet = new HashSet<char>();

        private void Init()
        {
            Logger.ErrorPath = ManuallyPath;
            if (File.Exists(ManuallyPath))
                File.Delete(ManuallyPath);
            if (File.Exists(BlackListPath))
                File.Delete(BlackListPath);
            File.Create(ManuallyPath).Close();
            File.Create(BlackListPath).Close();
            ReplaceArray = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.HighFreqErrorTagReplacement.txt", "OfflineAudioProcessingSystem")
                .Select(x => (x.Split('\t')[0], x.Split('\t')[1]))
                .ToArray();
            ValidTagSet = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.ValidTag.txt", "OfflineAudioProcessingSystem").ToHashSet();
        }
        private void ProcessFolder(string outputRootPath, string mappingPath)
        {
            foreach(var line in File.ReadLines(mappingPath).Select(x=>new MappingLine(x)))
            {
                OutputFile(outputRootPath, line.LocalPath, line);
            }
        }
        private void ValidateFolder(string inputRootPath)
        {
            foreach (string filePath in Directory.EnumerateFiles(inputRootPath, "*.txt", SearchOption.AllDirectories))
            {
                ValidateFile(filePath);
                ValidateFileAll(filePath);
            }
        }

        public void UpdateFolder(string inputRootPath, string outputRootPath, HashSet<string> validKeySet, bool createOutputWave)
        {
            List<string> mappingList = new List<string>();
            Dictionary<string, int> dict = new Dictionary<string, int>()
            {
                {"basel",87 },
                {"bern",64 },
                {"chur",75 },
                {"luzern",57 },
                {"stgallen",49 },
                {"visp",49 },
                {"zurich",31 },
            };
            foreach(string taskFolderPath in Directory.EnumerateDirectories(inputRootPath))
            {
                string taskName = taskFolderPath.Split('\\').Last();
                string taskId = taskName.Split('_')[0];
                string speakerFolder = Path.Combine(taskFolderPath, "Speaker");
                string locale = GetLocale(taskFolderPath);
                string speakerId = taskName.Split('_').Last();
                if (!(speakerId.Length == 5 && speakerId.All(x => x >= '0' && x <= '9')))
                    speakerId = "";
                string localePath = Path.Combine(outputRootPath, locale);
                Directory.CreateDirectory(localePath);
                foreach (string textPath in Directory.EnumerateFiles(speakerFolder, "*.txt"))
                {
                    string audioName = textPath.Split('\\').Last().Replace(".txt",".wav");
                    string alterName = audioName.Replace("ü", "u_").Replace(".txt",".wav");
                    string key = $"{taskId}\t{audioName}";
                    string alterKey = $"{taskId}\t{alterName}";
                    if (validKeySet.Contains(key) || validKeySet.Contains(alterKey)||validKeySet.Count==0)
                    {
                        var list = ValidateFile(textPath);
                        if (list != null && list.Count > 0)
                        {
                            var outputList = list.Select(x => OutputTransLine(x));
                            string fileName;
                            if (speakerId == "")
                            {
                                if (dict.ContainsKey(locale))
                                    dict[locale]++;
                                else
                                    dict[locale] = 0;
                                string alterSpeakerId = dict[locale].ToString("00000");
                                fileName = $"{locale}_{alterSpeakerId}_00000";                                
                            }
                            else
                            {
                                string alterAudioId = textPath.Split('\\').Last().Split('.')[0];
                                fileName = $"{locale}_{speakerId}_{alterAudioId}";
                            }
                            string outputTextPath = Path.Combine(localePath, fileName + ".txt");
                            File.WriteAllLines(outputTextPath, outputList);
                            string inputWavPath = textPath.Replace(".txt", ".wav");
                            string outputWavPath = Path.Combine(localePath, fileName + ".wav");
                            if (createOutputWave)
                                File.Copy(inputWavPath, outputWavPath);
                            mappingList.Add($"{textPath}\t{outputTextPath}");
                        }
                    }                        
                }
            }
            File.WriteAllLines(MappingPath, mappingList);
        }

        public void MergeTextGrid(string textGridFolder, string audioFolder, string outputFolder, string reportPath)
        {
            List<string> reportList = new List<string>();
            foreach(string taskFolder in Directory.EnumerateDirectories(textGridFolder))
            {
                string taskName = taskFolder.Split('\\').Last();
                string audioTaskFolder = Path.Combine(audioFolder, taskName);
                string outputTaskFolder = Path.Combine(outputFolder, taskName, "Speaker");
                Directory.CreateDirectory(outputTaskFolder);
                foreach (string tgFilePath in Directory.EnumerateFiles(taskFolder))
                {
                    FileInfo tgfile = new FileInfo(tgFilePath);
                    string fileName = tgfile.Name.Replace(tgfile.Extension, "");
                    string wavePath = Path.Combine(audioTaskFolder, fileName + ".wav");
                    string outputTextFilePath = Path.Combine(outputTaskFolder, fileName + ".txt");
                    string outputWaveFilePath = Path.Combine(outputTaskFolder, fileName + ".wav");
                    TextGrid.TextGridToText(tgFilePath, outputTextFilePath);
                    if (!TextGrid.Reject)
                    {
                        if (File.Exists(outputWaveFilePath))
                            File.Delete(outputWaveFilePath);
                        File.Copy(wavePath, outputWaveFilePath);
                    }
                    else
                    {
                        reportList.AddRange(TextGrid.AllList.Select(x => $"{tgFilePath}\t{x}"));
                    }
                }
            }
            File.WriteAllLines(reportPath, reportList);
        }

        private string GetLocale(string folderPath)
        {
            string id = folderPath.Split('\\').Last().Split('_')[0];
            if (id == "631")
                return "bern";
            var rawName = new string(folderPath.Split('\\').Last().ToLower()
                .Where(x => 'a' <= x && x <= 'z').ToArray());
            if (rawName.Contains("chur"))
                return "chur";
            if (rawName.Contains("basel"))
                return "basel";
            if (rawName.Contains("bern"))
                return "bern";
            if (rawName.Contains("visp"))
                return "visp";
            if (rawName.Contains("luzern"))
                return "luzern";
            if (rawName.Contains("lucern"))
                return "luzern";
            if (ContainsAll(rawName,'v','i','s','p'))
                return "visp";
            if (ContainsAll(rawName, 'l', 'u', 'z', 'e', 'r', 'n') || ContainsAll(rawName, 'l', 'u', 'c', 'e', 'r', 'n'))
                return "luzern";
            if (ContainsAll(rawName, 'z','u', 'r', 'i', 'c', 'h')||ContainsAll(rawName, 'z', 'ü', 'r', 'i', 'c', 'h'))
                return "zurich";
            if (ContainsAll(rawName, 's', 't', 'g', 'a', 'l', 'e', 'n'))
                return "stgallen";
            if (rawName.StartsWith("id"))
                return "stgallen";
            throw new CommonException(rawName);
        }

        private bool ContainsAll(string s, params char[] candidates)
        {
            foreach(char c in candidates)
            {
                if (!s.Contains(c))
                    return false;
            }
            return true;
        }

        string[] TransErrorArray =
        {
            "Succeed",                  //  0
            "Empty file.",              //  1
            "Missing time stamp.",      //  2
            "Time stamp format error.", //  3
            "Wrong speaker Id.",        //  4
            "Wrong dialect fix.",       //  5        
            "Unfinished.",              //  6
            "Empty content.",           //  7
            "Line count is not even.",  //  8
            "Time stamp mismatch.",     //  9
            "Content with speaker Id.", //  10
            "Content with dialect tag.",    //  11
            "Content with incomplete tag.", //  12
            "Digit.",            //  13
            "Non-digit char.",         //  14
            "No speaker ID",    //  15
        };

        string TimeStampString = "";
        private void ValidateFileAll(string filePath)
        {
            var r = File.ReadLines(filePath).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            string taskNameContent = GetTaskFormat(filePath);
            List<TransLine> list = new List<TransLine>();
            try
            {
                Sanity.Requires(r.Length > 2, filePath, 1);
                foreach(string s in r)
                {
                    TransLine line = new TransLine();
                    TimeStampString = "";
                    try
                    {
                        line = ExtractLine(s);
                        line.Content = ValidateContent(line.Content);
                        list.Add(line);
                    }
                    catch(CommonException e)
                    {
                        string content = $"{filePath}\t{taskNameContent}\t{TransErrorArray[e.HResult]}\t{TimeStampString}";                        
                        FullList.Add(content);                        
                    }
                }
            }
            catch(CommonException e)
            {
                string content = $"{filePath}\t{taskNameContent}\t{TransErrorArray[e.HResult]}";
                FullList.Add(content);
            }
        }
        private List<TransLine> ValidateFile(string filePath)
        {
            var r = File.ReadLines(filePath).Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
            List<TransLine> list = new List<TransLine>();
            try
            {
                Sanity.Requires(r.Length > 2, filePath, 1);
                foreach (string s in r)
                {
                    try
                    {
                        var line = ExtractLine(s);
                        line.Content = ValidateContent(line.Content);
                        list.Add(line);
                    }
                    catch (CommonException e)
                    {
                        switch (e.HResult)
                        {
                            case 12:    //Content with incomplete tag
                            case 14:    //Non-number chars.
                                string content = $"{TransErrorArray[e.HResult]}\t{e.Message}\t{filePath}";
                                Logger.WriteLine(content, false, true);                                
                                break;
                            default:
                                throw e;
                        }
                    }
                }
                string wavePath = filePath.ToLower().Replace(".txt", ".wav");
                ValidateTime(wavePath, list);
                ValidateListPair(list);
            }
            catch (CommonException e)
            {
                switch (e.HResult)
                {                        
                    case 1: //  Empty file  
                    case 2: //  Missing time stamp
                    case 4: //  Wrong speaker id.
                    case 6: //  Unfinished.
                    case 7: //  Empty content.
                    case 8: //  Count is not even,
                    case 9: //  Time stamp mismatch, 
                    case 10:    //Content with speaker id
                    case 11:    //Content with dialect tag   
                    case 13:    //Invalid char
                    case 15:    //No speaker ID
                        Logger.ErrorPath = BlackListPath;
                        Logger.WriteLine($"{TransErrorArray[e.HResult]}\t{e.Message}\t{filePath}",false,true);
                        Logger.ErrorPath = ManuallyPath;
                        return null;
                    case 3: //  Time stamp format
                    case 5: //  Wrong dialect fix. 
                        string content = $"{TransErrorArray[e.HResult]}\t{e.Message}\t{filePath}";
                        Logger.WriteLine(content, false, true);
                        return null;
                    default:
                        throw e;
                }
            }
            catch (Exception e)
            {
                string content = $"{SpaceReg.Replace(e.Message, " ")}\t{filePath}";
                Logger.WriteLine(content, false, true);
                return null;
            }
            return list;
        }
        private void OutputFile(string outputRootPath, string filePath, MappingLine mappingLine)
        {
            var list = ValidateFile(filePath);
            string name = $"{mappingLine.Dialect.ToLower()}_{mappingLine.SpeakerId}_00000";
            string inputWavePath = filePath.ToLower().Replace(".txt", ".wav");

            string outputFolder = Path.Combine(outputRootPath, mappingLine.Dialect.ToLower());
            Directory.CreateDirectory(outputFolder);
            string outputTextPath = Path.Combine(outputFolder,  name + ".txt");
            string outputWavPath = Path.Combine(outputFolder, name + ".wav");

            var outputList = list.Select(x => OutputTransLine(x));
            File.Copy(inputWavePath, outputWavPath, true);
            File.WriteAllLines(outputTextPath, outputList);
        }        

        Regex TransLineReg = new Regex("^(\\[.*?\\])(.*)$", RegexOptions.Compiled);
        Regex SpaceReg = new Regex("\\s+", RegexOptions.Compiled);
        Regex ExtraSpaceReg = new Regex(">\\s+<", RegexOptions.Compiled);

        private TransLine ExtractLine(string s)
        {
            s = SpaceReg.Replace(s, " ").Trim();
            TransLine line = new TransLine();
            line = SetTimeStamp(s, line);
            line = SetSpeakerId(line.Content, line);
            line = SetDialectFix(line.Content, line);
            return line;
        }

        private TransLine SetTimeStamp(string s, TransLine line)
        {
            string subContent = s.Substring(0, Math.Min(MAX_SHOW_COUNT, s.Length));
            Sanity.Requires(TransLineReg.IsMatch(s), subContent, 2);
            var groups = TransLineReg.Match(s).Groups;
            string timeStampString = groups[1].Value;

            Sanity.ReThrow(() =>
            {
                string core = timeStampString.Substring(1, timeStampString.Length - 2);
                string startString = core.Split(' ')[0];
                string endString = core.Split(' ')[1];
                double startTime = double.Parse(startString);
                double endTime = double.Parse(endString);
                line.StartTime = startTime;
                line.EndTime = endTime;
            }, new CommonException(subContent, 3));
            TimeStampString = $"{line.StartTime}\t{line.EndTime}";
            line.Content = groups[2].Value;
            Sanity.Requires(!string.IsNullOrWhiteSpace(line.Content), "", 7);
            return line;
        }
        private TransLine SetSpeakerId(string s, TransLine line)
        {
            s = s.Trim();
            string lower = s.ToLower();
            if (lower == "<unknown/>")
            {
                line.Speaker = "";
                line.Content = s;
                return line;
            }
            Sanity.Requires(lower.Contains("s1") || lower.Contains("s2"), line.StartTime.ToString(), 15);
            if (!lower.StartsWith("s1") && lower.StartsWith("s2"))
                ;
            Sanity.Requires(lower.StartsWith("s1") || lower.StartsWith("s2"), line.StartTime.ToString(), 4);
            line.Speaker = s.Substring(0, 2).ToUpper();
            line.Content = s.Substring(2).Trim();
            return line;
        }
        const string O_PREFIX = "<chdialects>";
        const string O_SUFFIX = "<chdialects/>";
        const string C_PREFIX = "<chdialects-converted>";
        const string C_SUFFIX = "<chdialects-converted/>";
        private TransLine SetDialectFix(string s, TransLine line)
        {
            if (s.Trim().ToLower() == "<unknown/>")
                return line;
            string subContent = s.Substring(0, Math.Min(MAX_SHOW_COUNT, s.Length));
            if (s.StartsWith(O_PREFIX) && s.EndsWith(O_SUFFIX))
                return GetMiddle(s, O_PREFIX, O_SUFFIX, line);
            if (s.StartsWith(C_PREFIX) && s.EndsWith(C_SUFFIX))
                return GetMiddle(s, C_PREFIX, C_SUFFIX, line);
            if (s.StartsWith(O_PREFIX))
            {
                if (!s.EndsWith(O_PREFIX))
                    return SetDialectFix($"{s}{O_SUFFIX}", line);
                string middle = s.Replace(O_PREFIX, "").Trim();
                line.Prefix = O_PREFIX;
                line.Content = middle;
                line.Suffix = O_SUFFIX;
                return line;                
            }
            if (s.StartsWith(C_PREFIX))
            {
                if (!s.EndsWith(C_PREFIX))
                    return SetDialectFix($"{s}{C_SUFFIX}", line);
                string middle = s.Replace(C_PREFIX, "").Trim();
                line.Prefix = C_PREFIX;
                line.Content = middle;
                line.Suffix = C_SUFFIX;
                return line;                
            }
            throw new CommonException(subContent, 5);
        }
        private TransLine GetMiddle(string total, string prefix, string suffix, TransLine line)
        {
            string middle = total.Substring(prefix.Length, total.Length - prefix.Length - suffix.Length).Trim();
            middle = middle.Replace(prefix, "").Replace(suffix, "");
            line.Prefix = prefix;
            line.Content = middle;
            line.Suffix = suffix;
            return line;            
        }
        private void ValidateTime(string wavePath, List<TransLine> list)
        {
            Wave w = new Wave();
            w.ShallowParse(wavePath);
            double audioLength = w.AudioLength;

            double maxTimeStamp = list.Max(x => x.EndTime);

            Sanity.Requires(maxTimeStamp >= 0 && (audioLength - maxTimeStamp <= 10 || audioLength / maxTimeStamp <= 2), wavePath, 6);
        }
        private string OutputTransLine(TransLine line)
        {
            return $"[{line.StartTime} {line.EndTime}] {line.Speaker} {line.Prefix} {line.Content} {line.Suffix}";
        }

        private void ValidateListPair(List<TransLine> list)
        {
            Sanity.Requires(list.Count % 2 == 0, $"Count error.", 8);
            TransLine o = new TransLine { StartTime = double.MinValue, EndTime = double.MinValue + 1 };
            for(int i = 0; i * 2 < list.Count; i += 2)
            {
                Sanity.Requires(list[i].EndTime > 0, list[i].EndTime.ToString(), 9);
                Sanity.Requires(list[i].StartTime == list[i + 1].StartTime && list[i].EndTime == list[i + 1].EndTime, list[i].StartTime.ToString(), 9);
                Sanity.Requires(list[i].StartTime < list[i].EndTime, list[i].StartTime.ToString(), 9);
                Sanity.Requires(list[i].StartTime >= o.EndTime, list[i].StartTime.ToString(), 9);
            }
        }

        Regex TagReg = new Regex("<[^<]*?>", RegexOptions.Compiled);
                        
        private string ValidateContent(string content)
        {
            string subContent = content.Substring(0, Math.Min(content.Length, MAX_SHOW_COUNT));
            Sanity.Requires(!content.ToLower().Contains("s1") && !content.ToLower().Contains("s2"), subContent, 10);
            if (content.ToLower().Contains("chdialects"))
                ;
            Sanity.Requires(!content.ToLower().Contains("chdialects"), subContent, 11);
            foreach (var replace in ReplaceArray)
                content = content.Replace(replace.Item1, replace.Item2);
            content = content.Replace("?", "<questionmark>")
                .Replace(":", "<comma>")
                .Replace('`','\'')
                .Replace("’", "'")
                .Replace(",", "<comma>")
                .Replace(";", "<comma>")
                .Replace(".", "<fullstop>")
                .Replace("!", "<fullstop>")
                .Replace("´", "'")
                .Replace("，", "<comma>")
                .Replace("。", "<fullstop>")
                .Replace("\"", " ")
                .Replace("<unknown>","<UNKNOWN/>")
                ;
            

            var localTags = GetTags(content).ToArray();
            var diff = localTags.Except(ValidTagSet);
            Sanity.Requires(diff.Count() == 0, subContent, 12);
            TagSet.UnionWith(localTags);
            string rawContent = TagReg.Replace(content, " ")
                .Replace("-\\BINDESTRICH","")
                ;
            Sanity.Requires(!rawContent.Contains('<') && !rawContent.Contains('>'), subContent, 12);
            foreach(char c in rawContent)
            {
                if (c >= 'A' && c <= 'Z')
                    continue;
                if (c >= 'a' && c <= 'z')
                    continue;
                if (c >= '\u00c0' && c <= '\u017f')
                    continue;
                if (c == ' ' || c == '-' || c == '\'')
                    continue;
                if (c >= '0' && c <= '9')
                    throw new CommonException($"{c}", 13);
                throw new CommonException($"{c}", 14);
            }
            CharSet.UnionWith(rawContent);
            content = CleanupSpace(content);
            return content;
        }
        
        private IEnumerable<string> GetTags(string content)
        {
            return TagReg.Matches(content).Cast<Match>().Select(x => x.Groups[0].Value);
        }

        private string CleanupSpace(string content)
        {
            content = content.Replace("<", " <").Replace(">", "> ");
            content = ExtraSpaceReg.Replace(content, "><");
            content = SpaceReg.Replace(content, " ").Trim();
            
            return content;
        }

        private string GetTaskFormat(string filePath)
        {
            try
            {
                var r = filePath.Split('\\').Reverse().ToArray();
                string audioName = r[0].Replace(".txt", "");
                string taskName = r[2];
                return $"{taskName}\t{audioName}";
            }
            catch
            {
                return "";
            }
        }
    }  

    struct TransLine
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public string Speaker { get; set; }
        public string Content { get; set; }
    }

    class MappingLine : Line
    {
        public string LocalPath { get; set; }
        public string AzureName { get; set; }
        public string TaskId { get; set; }
        public string TaskInternalName { get; set; }
        public string Dialect { get; set; }
        public string SpeakerId { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public MappingLine() : base() { }
        public MappingLine(string lineStr) : base(lineStr) { }
        protected override IEnumerable<object> GetLine()
        {
            yield return LocalPath;
            yield return AzureName;
            yield return TaskId;
            yield return TaskInternalName;
            yield return Dialect;
            yield return SpeakerId;
            yield return Gender;
            yield return Age;
        }

        protected override void SetLine(string[] split)
        {
            LocalPath = split[0];
            AzureName = split[1];
            TaskId = split[3];
            TaskInternalName = split[4];
            Dialect = split[5];
            SpeakerId = int.Parse(split[6]).ToString("00000");
            Gender = split[7];
            Age = int.Parse(split[8]);
        }
    }
}
