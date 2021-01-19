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
            Init();            
        }
        HashSet<string> TagSet = new HashSet<string>();
        (string, string)[] ReplaceArray = new (string, string)[0];
        HashSet<string> ValidTagSet = new HashSet<string>();
        HashSet<char> CharSet = new HashSet<char>();

        private void Init()
        {
            Logger.ErrorPath = @"";
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
                ValidateFile(filePath);
        }
        string[] TransErrorArray =
        {
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
            "Invalid char.",            //  13
        };
        
        private List<TransLine> ValidateFile(string filePath)
        {
            var r = File.ReadAllLines(filePath);
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
                            case 10:    //Content with speaker id
                            case 11:    //Content with dialect tag
                            case 12:    //Content with incomplete tag
                            case 13:    //Invalid char
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
                    case 3: //  Time stamp format
                    case 4: //  Wrong speaker id.
                    case 5: //  Wrong dialect fix. 
                    case 6: //  Unfinished.
                    case 7: //  Empty content.
                    case 8: //  Count is not even,
                    case 9: //  Time stamp mismatch,
                        string content = $"{TransErrorArray[e.HResult]}\t{e.Message}\t{filePath}";
                        Logger.WriteLine(content, false, true);
                        return null;
                    default:
                        throw e;
                }
            }
            catch (Exception e)
            {
                string content = $"{e.Message}\t{filePath}";
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
            Sanity.Requires(TransLineReg.IsMatch(s), s.Substring(0, 30), 2);
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
            }, new CommonException(s.Substring(0, 30), 3));
            line.Content = groups[2].Value;
            Sanity.Requires(!string.IsNullOrWhiteSpace(line.Content), "", 7);
            return line;
        }
        private TransLine SetSpeakerId(string s, TransLine line)
        {            
            string lower = s.ToLower();
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
            throw new CommonException(s.Substring(0, 30), 5);
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
            Sanity.Requires(!content.ToLower().Contains("s1") && !content.ToLower().Contains("s2"), content.Substring(0, 30), 10);
            Sanity.Requires(!content.ToLower().Contains("chdialects"), content.Substring(0, 30), 11);
            foreach (var replace in ReplaceArray)
                content = content.Replace(replace.Item1, replace.Item2);
            content = content.Replace("?", "<questionmark>")
                .Replace(",", "<comma>")
                .Replace(";", "<comma>")
                .Replace(".", "<fullstop>")
                .Replace("!", "<fullstop>")
                .Replace("´", "'");

            var localTags = GetTags(content).ToArray();
            var diff = localTags.Except(ValidTagSet);
            Sanity.Requires(diff.Count() == 0, content.Substring(0, 30), 12);
            TagSet.UnionWith(localTags);
            string rawContent = TagReg.Replace(content, " ");
            Sanity.Requires(!rawContent.Contains('<') && !rawContent.Contains('>'), content.Substring(0, 30), 12);
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
                throw new CommonException($"{c}", 13);
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
            TaskId = split[2];
            TaskInternalName = split[3];
            Dialect = split[4];
            SpeakerId = int.Parse(split[5]).ToString("00000");
            Gender = split[6];
            Age = int.Parse(split[7]);
        }
    }
}
