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
    class TranscriptValidation:Feature
    {
        ConfigTranscriptValidation Cfg = new ConfigTranscriptValidation();
        protected override void LoadConfig(string configPath)
        {
            Cfg.Load(configPath);
        }

        protected override void Run()
        {
            TranscriptsTransfer tt = new TranscriptsTransfer("char.txt");
            tt.Run(@"D:\WorkFolder\Transcripts\20201224\Input", @"D:\WorkFolder\Transcripts\20201224\Output");
        }
    }

    class TranscriptsTransfer : FolderTransfer
    {
        Regex LineReg = new Regex("(^(s|S)[0-9]+)(.*)$", RegexOptions.Compiled);
        Regex TagReg = new Regex("<.*?>", RegexOptions.Compiled);
        HashSet<string> ValidRegSet = new HashSet<string>();
        (string, string)[] HighFreqReplacements = new (string, string)[0];
        List<string> InvalidTags = new List<string>();
        HashSet<char> CharSet = new HashSet<char>();
        string CharPath = "";
        public TranscriptsTransfer(string charPath) : base()
        {
            CharPath = charPath;
        }
        protected override void PreRun()
        {
            ValidRegSet = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.ValidTag.txt", "OfflineAudioProcessingSystem").ToHashSet();
            HighFreqReplacements = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.HighFreqErrorTagReplacement.txt", "OfflineAudioProcessingSystem")
                .Select(x=>(x.Split('\t')[0],x.Split('\t')[1]))
                .ToArray();
        }
        protected override IEnumerable<string> GetItems(string inputFolderPath)
        {
            return Directory.EnumerateFiles(inputFolderPath, "*.txt");
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            FileInfo file = new FileInfo(inputPath);
            string wavePath = file.FullName.Replace(file.Extension, ".wav");
            try
            {
                ProcessFile(inputPath, wavePath, outputPath);
                if (File.Exists(outputPath))
                {
                    FileInfo outputFile = new FileInfo(outputPath);
                    string outputWavePath = outputFile.FullName.Replace(file.Extension, ".wav");
                    //File.Copy(wavePath, outputWavePath);
                }
            }
            catch(CommonException e)
            {
                Logger.WriteLineWithLock(e.Message, false, true);
            }
        }

        private void ProcessFile(string transPath, string wavPath, string outputFilePath)
        {
            List<TranscriptionLine> list = new List<TranscriptionLine>();
            double maxEnd = -1;
            var transcriptContent = File.ReadAllLines(transPath);
            //Sanity.Requires(transcriptContent.Length > 0, $"Empty file\t{transPath}");
            if (transcriptContent.Length == 0)
                return;
            foreach (string s in transcriptContent)
            {
                string timeStamp="";
                try
                {                    
                    var line = StringToTransLine(s, out timeStamp);
                    if (line.TS.EndTime > maxEnd)
                        maxEnd = line.TS.EndTime;
                    list.Add(line);
                }
                catch(CommonException e)
                {
                    if (e.Message != "")
                        Logger.WriteLineWithLock(string.Join("\t", e.Message, timeStamp, transPath), false, true);
                    if (e.Message[0] == '*')
                        return;
                }
            }
            Wave w = new Wave();
            w.ShallowParse(wavPath);
            //Sanity.Requires(maxEnd > 0, $"Audio length error\t{transPath}");
            Sanity.Requires(maxEnd > 0, $"");
            if (w.AudioLength/maxEnd >= 2)
            {
                //Sanity.Requires(w.AudioLength - maxEnd <= 10, $"Audio length error\t{transPath}");
                Sanity.Requires(w.AudioLength - maxEnd <= 10, $"");
            }
            ValidatePair(list, transPath);
            File.WriteAllLines(outputFilePath, list.Select(x => x.Output()));
        }

        private TranscriptionLine StringToTransLine(string s, out string timeStamp)
        {
            //Sanity.Requires(s[0] == '[', "*Not start with '['.");
            //Sanity.Requires(s.Contains(']'), "*No right bracket ']' inside.");
            Sanity.Requires(s[0] == '[', "");
            Sanity.Requires(s.Contains(']'), "");
            TranscriptionLine line = new TranscriptionLine();
            line.Sep = ' ';
            string timeStampString = s.Substring(0, s.IndexOf(']') + 1);
            TimeStamp ts = new TimeStamp(timeStampString);
            line.TS = ts;
            timeStamp = ts.ToString();
            string tail = s.Substring(s.IndexOf(']') + 1).Trim();
            if (string.IsNullOrEmpty(tail))
            {
                line.SpeakerId = "";
                line.Content = "";
                return line;
            }
            if (!LineReg.IsMatch(tail))
            {
                line.SpeakerId = "S1";
                line.Content = tail;
            }
            else
            {
                line.SpeakerId = LineReg.Match(tail).Groups[1].Value;
                line.Content = LineReg.Match(tail).Groups[3].Value.Trim();
            }

            line.Content = ReplaceHighFreq(line.Content);
            string fixedContent;
            Sanity.Requires(DialectTag(line.Content,out fixedContent) != 0, "*Missing dialect tags.");
            line.Content = fixedContent;
            if (string.IsNullOrWhiteSpace(line.Content))
                ;
            var tags = TagReg.Matches(line.Content).Cast<Match>().Select(x => x.Groups[0].Value);
            var extra = tags.Except(ValidRegSet).ToArray();

            Sanity.Requires(extra.Length == 0, $"Invalid tags {string.Join(" ", extra)}");

            string rawContent = TagReg.Replace(line.Content, "");
            bool valid = !rawContent.Contains('<') && !rawContent.Contains('>');
            if (!valid)
                ;
            Sanity.Requires(valid, "Incomplete tags.");

            return line;
        }
        private int DialectTag(string s, out string outputString)
        {
            outputString = s;
            if (s.StartsWith("<chdialects>") && s.EndsWith("<chdialects/>"))
            {
                return 1;
            }
            if (s.StartsWith("<chdialects-converted>") && s.EndsWith("<chdialects-converted/>"))
            {
                return -1;
            }
            if (s.StartsWith("<chdialects>"))
            {
                outputString = $"{s}<chdialects/>";
                return 1;
            }
            if (s.StartsWith("<chdialects-converted>"))
            {
                outputString = $"{s}<chdialects-converted/>";
                return -1;
            }
            return 0;
        }

        private string ReplaceHighFreq(string s)
        {
            foreach(var replacement in HighFreqReplacements)
            {
                s = s.Replace(replacement.Item1, replacement.Item2);
            }
            return s;
        }

        protected override void PostRun()
        {
            File.WriteAllLines(CharPath, CharSet.OrderBy(x => x).Select(x => x.ToString()));
        }

        private void ValidatePair(List<TranscriptionLine> list, string path)
        {
            //List<(TranscriptionLine, TranscriptionLine)> outputList = new List<(TranscriptionLine, TranscriptionLine)>();
            if (list.Count % 2 != 0)
                ;
            Sanity.Requires(list.Count % 2 == 0, $"Count error\t{path}");
            TranscriptionLine o = null;
            TranscriptionLine c = null;
            for(int i = 0; i < list.Count - 1; i++)
            {
                var line = list[i];
                Sanity.Requires(!string.IsNullOrWhiteSpace(line.Content), $"Empty line\t{path}");
                if (i % 2 == 0)
                {
                    Sanity.Requires(line.Content.StartsWith("<chdialects>"), $"Not original\t{path}");
                    Sanity.Requires(o == null || line.TS.StartTime >= line.TS.EndTime, $"Time stamp mismatch\t{path}");
                    o = line;                    
                }
                else
                {
                    Sanity.Requires(line.Content.StartsWith("<chdialects-converted>"), $"Not converted\t{path}");
                    c = line;
                    Sanity.Requires(o.TS.StartTime == c.TS.StartTime, $"Start mismatch\t{path}");
                    Sanity.Requires(o.TS.EndTime == c.TS.EndTime, $"End mismatch\t{path}");
                }
            }            
        }
    }
    
    class TimeStamp
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public TimeStamp(string s)
        {
            string core = s.Substring(1, s.Length - 2);
            try
            {
                StartTime = double.Parse(core.Split(' ','\u00a0')[0]);
                EndTime = double.Parse(core.Split(' ', '\u00a0')[1]);
            }
            catch
            {
                var r = core.ToArray();
            }
            Sanity.Requires(EndTime >= StartTime, "Endtime less than start time.");
        }

        public override string ToString()
        {
            return $"[{StartTime} {EndTime}]";
        }
    }
    class TranscriptionLine : Line
    {
        public TimeStamp TS { get; set; }
        public string SpeakerId { get; set; }
        public string Content { get; set; }
        public int Converted { get; set; } = 0;
        protected override IEnumerable<object> GetLine()
        {
            yield return TS.ToString();
            yield return SpeakerId;
            yield return Content;
        }

        protected override void SetLine(string[] split)
        {
            throw new CommonException("Transcription line doesn't support direct SetLine.");
        }

        private void SetConverted()
        {
            if (Content.Length == 0)
            {
                Converted = 0;
                return;
            }
            if (Content.StartsWith("<chdialects>") && Content.EndsWith("<chdialects/>"))
            {
                Converted = -1;
                return;
            }
            if(Content.StartsWith("<chdialects-converted>") && Content.EndsWith("<chdialects-converted/>"))
            {
                Converted = 1;
                return;
            }
            throw new CommonException("Invalid head tail.");
        }
    }


    class NewTransValidation
    {
        public NewTransValidation()
        {
            BrowseFolder();
        }
        List<string> DialectTagList = new List<string>();
        HashSet<string> BlackList = new HashSet<string>();
        HashSet<string> TagSet = new HashSet<string>();
        HashSet<string> IncompleteSet = new HashSet<string>();
        private void BrowseFolder()
        {
            string rootPath = @"D:\WorkFolder\Transcripts\20201224\Input";
            string outputPath = @"D:\WorkFolder\Transcripts\20201224\result.txt";
            string dialectPath = @"D:\WorkFolder\Transcripts\20201224\Dialect.txt";
            string blackListPath = @"D:\WorkFolder\Transcripts\20201224\BlackList.txt";
            List<string> overallList = new List<string>();
            int[] array = new int[11];
            BlackList = File.ReadLines(blackListPath)
                .Select(x => x.Split('\t')[0].ToLower())
                .ToHashSet();
            foreach(string filePath in Directory.EnumerateFiles(rootPath, "*.txt", SearchOption.AllDirectories))
            {
                if (BlackList.Contains(filePath.ToLower()))
                    continue;
                int hresult = ValidateFile(filePath);
                if (hresult != 1 && hresult != 6)
                    overallList.Add($"{hresult}\t{filePath}");
                if (hresult != -1)
                    array[hresult]++;
            }
            File.WriteAllLines(outputPath, overallList.OrderBy(x => x));
            File.WriteAllLines(dialectPath, DialectTagList);
            foreach(int i in array)
                Console.WriteLine(i);
        }
        private int ValidateFile(string filePath)
        {
            var r = File.ReadAllLines(filePath);
            List<TransLine> list = new List<TransLine>();
            try
            {
                Sanity.Requires(r.Length >2, "Empty file", 1);
                foreach (string s in r)
                {
                    var line = ExtractLine(s);
                    list.Add(line);
                }
                string wavePath = filePath.ToLower().Replace(".txt", ".wav");
                ValidateTime(wavePath, list);
                ValidateList(list);
                if(list.Count<20)
                    Console.WriteLine(filePath);
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
            Sanity.Requires(WithTimeStamp(s), "Missing time stamp.", 2);
            TransLine line = new TransLine();
            s = SpaceReg.Replace(s, " ").Trim();

            Sanity.Requires(TransLineReg.IsMatch(s), "Missing time stamp.", 2);
            
            string timeStampString = TransLineReg.Match(s).Groups[1].Value;

            try
            {
                var timeStamp = GetTimeStamp(timeStampString);
                line.StartTime = timeStamp.Item1;
                line.EndTime = timeStamp.Item2;
            }
            catch
            {
                throw new CommonException("Time stamp format", 3);
            }

            string tail = TransLineReg.Match(s).Groups[2].Value;
            Sanity.Requires(!string.IsNullOrWhiteSpace(tail), "Empty in content", 7);

            int index;
            line.Speaker = GetSpeakerId(tail, out index);

            string totalContent = tail.Substring(index).Trim();
            var parts = ExtractFix(totalContent);
            line.Prefix = parts.Item1;
            line.Content = parts.Item2;
            line.Suffix = parts.Item3;
            return line;
        }
        private (double,double) GetTimeStamp(string s)
        {
            string core = s.Substring(1, s.Length - 2);
            string startString = core.Split(' ')[0];
            string endString = core.Split(' ')[1];
            return (double.Parse(startString), double.Parse(endString));
        }
        private string GetSpeakerId(string tail, out int index)
        {
            index = 2;
            string lower = tail.ToLower();
            Sanity.Requires(lower.StartsWith("s1") || lower.StartsWith("s2"), "Wrong speakerId", 4);
            if (lower.StartsWith("s1") || lower.StartsWith("s2"))
                return tail.Substring(0, 2).ToUpper();
            index = 0;
            return "S1";
        }
        const string O_PREFIX = "<chdialects>";
        const string O_SUFFIX = "<chdialects/>";
        const string C_PREFIX = "<chdialects-converted>";
        const string C_SUFFIX = "<chdialects-converted/>";
        private (string,string,string) ExtractFix(string totalContent)
        {
            if (totalContent.StartsWith(O_PREFIX) && totalContent.EndsWith(O_SUFFIX))
                return GetMiddle(totalContent, O_PREFIX, O_SUFFIX);
            if (totalContent.StartsWith(C_PREFIX) && totalContent.EndsWith(C_SUFFIX))
                return GetMiddle(totalContent, C_PREFIX, C_SUFFIX);
            if (totalContent.StartsWith(O_PREFIX))
                return ExtractFix($"{totalContent}{O_SUFFIX}");
            if (totalContent.StartsWith(C_PREFIX))
                return ExtractFix($"{totalContent}{C_SUFFIX}");
            throw new CommonException(totalContent, 5);
        }
        private (string,string,string) GetMiddle(string total, string prefix, string suffix)
        {
            string middle = total.Substring(prefix.Length, total.Length - prefix.Length - suffix.Length).Trim();
            return (prefix, middle, suffix);
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
            return $"[{line.StartTime} {line.EndTime}] {line.Speaker} {line.Prefix}{line.Content}{line.Suffix}";
        }

        private void ValidateList(List<TransLine> list)
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

        Regex TagReg = new Regex("<.*?>", RegexOptions.Compiled);
        Regex ImcompleteReg = new Regex("");
        private string CleanupContent(string content)
        {
            var regs = GetTags(content);
            TagSet.UnionWith(regs);
            string rawContent = TagReg.Replace(content, " ");

            return "";
        }
        
        private IEnumerable<string> GetTags(string content)
        {
            return TagReg.Matches(content).Cast<Match>().Select(x => x.Groups[0].Value);
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
}
