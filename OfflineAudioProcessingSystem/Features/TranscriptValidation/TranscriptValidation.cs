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
        public TranscriptValidation()
        {
            //BrowseFolder();
        }
        List<string> DialectTagList = new List<string>();
        HashSet<string> BlackList = new HashSet<string>();
        HashSet<string> TagSet = new HashSet<string>();
        (string, string)[] ReplaceArray = new (string, string)[0];
        HashSet<string> ValidTagSet = new HashSet<string>();
        HashSet<char> CharSet = new HashSet<char>();
        private void BrowseFolder()
        {
            string rootPath = @"D:\WorkFolder\Transcripts\20201224\Input";
            string blackListPath = @"D:\WorkFolder\Transcripts\20201224\BlackList.txt";
            string validFilePath= @"D:\WorkFolder\Transcripts\20201224\Valid.txt";
            string mappingPath = @"D:\WorkFolder\Transcripts\20201224\Mapping.txt";
            Dictionary<string, MappingLine> mappingDict = new Dictionary<string, MappingLine>();
            List<string> overallList = new List<string>();
            List<string> validFileList = new List<string>();
            int[] array = new int[14];
            BlackList = File.ReadLines(blackListPath)
                .Select(x => x.Split('\t')[0].ToLower())
                .ToHashSet();
            ReplaceArray= IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.HighFreqErrorTagReplacement.txt", "OfflineAudioProcessingSystem")
                .Select(x => (x.Split('\t')[0], x.Split('\t')[1]))
                .ToArray();
            ValidTagSet= IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.ValidTag.txt", "OfflineAudioProcessingSystem").ToHashSet();
            mappingDict = File.ReadLines(mappingPath).ToDictionary(x => x.Split('\t')[0].ToLower(), x => new MappingLine(x));
            foreach (string filePath in Directory.EnumerateFiles(rootPath, "*.txt", SearchOption.AllDirectories))
            {
                if (!mappingDict.ContainsKey(filePath.ToLower()))
                    continue;
                var line = mappingDict[filePath.ToLower()];
                
                if (BlackList.Contains(filePath.ToLower()))
                    continue;
                int hresult = ValidateFile(filePath, line);
                if (hresult != 1 && hresult != 6)
                    overallList.Add($"{hresult}\t{filePath}");
                if (hresult != -1)
                    array[hresult]++;

                if (hresult == 0)
                    validFileList.Add(filePath);

            }
            File.WriteAllLines(validFilePath, validFileList);
        }
        private int ValidateFile(string filePath, MappingLine mappingLine)
        {
            var r = File.ReadAllLines(filePath);
            List<TransLine> list = new List<TransLine>();
            try
            {
                Sanity.Requires(r.Length >2, "Empty file", 1);
                foreach (string s in r)
                {
                    try
                    {
                        var line = ExtractLine(s);
                        line.Content = ValidateContent(line.Content);
                        list.Add(line);
                    }
                    catch(CommonException e)
                    {
                        switch (e.HResult)
                        {
                            case 10:
                            case 11:
                            case 12:
                            case 13:
                                Console.WriteLine($"{e.Message}\t{filePath}");
                                break;
                            default:
                                break;
                        }
                    }
                }
                string wavePath = filePath.ToLower().Replace(".txt", ".wav");
                ValidateTime(wavePath, list);
                ValidateListPair(list);
            }
            catch(CommonException e)
            {
                switch (e.HResult)
                {
                    case 1: //  Empty file                    
                    case 2: //  Missing time stamp
                    case 3: //  Time stamp format
                    case 4: //  Wrong speaker id.
                    case 5: //  Wrong dialect fix. 
                    case 6: //  Unfinished.
                    case 7: //  Empty content.
                    case 8: //  Count is not even,
                    case 9: //  Time stamp mismatch,
                        return e.HResult;
                    default:
                        break;
                }
            }
            catch(Exception e)
            {
                return -1;
            }
            string outputRootPath = @"D:\WorkFolder\Transcripts\20201224\Output";
            string name = $"{mappingLine.Dialect}_{mappingLine.SpeakerId}_{mappingLine.SpeakerId}";
            string inputWavePath = filePath.ToLower().Replace(".txt", ".wav");
            
            string outputTextPath = Path.Combine(outputRootPath, name + ".txt");
            string outputWavPath = Path.Combine(outputRootPath, name + ".wav");

            var outputList = list.Select(x => OutputTransLine(x));
            File.Copy(inputWavePath, outputWavPath, true);
            File.WriteAllLines(outputTextPath, outputList);
            return 0;
        }
        private bool WithTimeStamp(string s)
        {
            return s.Contains('[') && s.Contains(']');
        }

        Regex TransLineReg = new Regex("^(\\[.*?\\])(.*)$", RegexOptions.Compiled);
        Regex SpaceReg = new Regex("\\s+", RegexOptions.Compiled);      

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
            Sanity.Requires(TransLineReg.IsMatch(s), "Missing time stamp", 2);
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
            }, new CommonException("Time stamp format error", 3));
            line.Content = groups[2].Value;
            Sanity.Requires(!string.IsNullOrWhiteSpace(line.Content), "Empty in content", 7);
            return line;
        }
        private TransLine SetSpeakerId(string s, TransLine line)
        {            
            string lower = s.ToLower();
            Sanity.Requires(lower.StartsWith("s1") || lower.StartsWith("s2"), "Wrong speakerId", 4);
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
            throw new CommonException(s, 5);
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

            Sanity.Requires(maxTimeStamp >= 0 && (audioLength - maxTimeStamp <= 10 || audioLength / maxTimeStamp <= 2), "Time mismatch", 6);
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
                Sanity.Requires(list[i].EndTime > 0, "Time stamp mismatch", 9);
                Sanity.Requires(list[i].StartTime == list[i + 1].StartTime && list[i].EndTime == list[i + 1].EndTime, "Time stamp mismatch.", 9);
                Sanity.Requires(list[i].StartTime < list[i].EndTime, "Time stamp mismatch", 9);
                Sanity.Requires(list[i].StartTime >= o.EndTime, "Time stamp mismatch", 9);
            }
        }

        Regex TagReg = new Regex("<[^<]*?>", RegexOptions.Compiled);
                        
        private string ValidateContent(string content)
        {            
            Sanity.Requires(!content.ToLower().Contains("s1") && !content.ToLower().Contains("s2"), "Content with speaker id", 10);
            Sanity.Requires(!content.ToLower().Contains("chdialects"), "Content with dialect tag", 11);
            foreach (var replace in ReplaceArray)
                content = content.Replace(replace.Item1, replace.Item2);
            content = content.Replace("?", "<questionmark>")
                .Replace(",", "<comma>")
                .Replace(";", "<comma>")
                .Replace(".", "<fullstop>")
                .Replace("!", "<fullstop>")
                .Replace("´", "'");

            var regs = GetTags(content);
            TagSet.UnionWith(regs);
            string rawContent = TagReg.Replace(content, " ");
            Sanity.Requires(!rawContent.Contains('<') && !rawContent.Contains('>'), "Content with incomplete tag", 12);
            foreach(char c in rawContent)
            {
                if (c >= 'A' && c <= 'Z')
                    continue;
                if (c >= 'a' && c <= 'z')
                    continue;
                if (c >= '\u00c0' && c <= '\u00ff')
                    continue;
                if (c == ' ' || c == '-' || c == '\'')
                    continue;                
                throw new CommonException($"Invalid char {c}", 13);
            }
            CharSet.UnionWith(rawContent);
            content = CleanupSpace(content);
            return content;
        }
        
        private IEnumerable<string> GetTags(string content)
        {
            return TagReg.Matches(content).Cast<Match>().Select(x => x.Groups[0].Value);
        }

        private MappingLine GenerateMappingLine(string s)
        {
            var split = s.Split('\t');
            string taskInternalName = split[0].Split('\\').Last().Replace(".txt", "");
            string dialect = split[1].ToLower();
            string gender = split[3].ToLower();
            int age = int.Parse(split[4]);
            string azureFolder = split[5];
            string azurePath = AzureUtils.SetDataUri(AzureUtils.PathCombine(azureFolder, taskInternalName.Replace(' ','+')+".wav"));

            string azureName = AzureUtils.BlobExists(azurePath) ?
                AzureUtils.GetShort( azurePath) :
                "";
            return new MappingLine
            {
                AzureName = azureName,
                TaskId = "",
                TaskInternalName = taskInternalName,
                Dialect = dialect,
                SpeakerId = "",
                Gender = gender,
                Age = age,
            };
        }

        private string CleanupSpace(string content)
        {
            content = content.Replace("<", " <").Replace(">", "> ");
            content = SpaceReg.Replace(content, " ").Trim();
            content = content.Replace("> <", "><");
            return content;
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
            SpeakerId = split[6];
            Gender = split[7];
            Age = int.Parse(split[8]);
        }
    }
}
