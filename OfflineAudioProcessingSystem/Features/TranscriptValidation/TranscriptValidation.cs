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
                    File.Copy(wavePath, outputWavePath);
                }
            }
            catch(CommonException e)
            {
                Logger.WriteLineWithLock(e.Message, false, true);
            }
        }

        private void ProcessFile(string transPath, string wavPath, string outputFilePath)
        {
            List<string> list = new List<string>();
            double maxEnd = -1;
            var transcriptContent = File.ReadAllLines(transPath);
            //Sanity.Requires(transcriptContent.Length > 0, $"Empty file\t{transPath}");
            if (transcriptContent.Length == 0)
                return;
            if (transcriptContent.Length % 2 != 0)
                return;
            foreach (string s in transcriptContent)
            {
                string timeStamp="";
                try
                {                    
                    var line = StringToTransLine(s, out timeStamp);
                    if (line.TS.EndTime > maxEnd)
                        maxEnd = line.TS.EndTime;
                    list.Add(line.Output());
                }
                catch(CommonException e)
                {
                    Logger.WriteLineWithLock(string.Join("\t", e.Message, timeStamp, transPath), false, true);
                    if (e.Message[0] == '*')
                        break;
                }
            }
            Wave w = new Wave();
            w.ShallowParse(wavPath);
            if (w.AudioLength - maxEnd > 1)
            {
                Sanity.Requires(w.AudioLength / maxEnd >= 0.5, $"Mismatch content\t{transPath}");
            }
            File.WriteAllLines(outputFilePath, list);
        }

        private TranscriptionLine StringToTransLine(string s, out string timeStamp)
        {
            Sanity.Requires(s[0] == '[', "*Not start with '['.");
            Sanity.Requires(s.Contains(']'), "*No right bracket ']' inside.");
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
            Sanity.Requires(DialectTag(line.Content) != 0, "*Missing dialect tags.");
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
        private int DialectTag(string s)
        {
            if (s.StartsWith("<chdialects>") && s.EndsWith("<chdialects/>"))
                return 1;
            if (s.StartsWith("<chdialects-converted>") && s.EndsWith("<chdialects-converted/>"))
                return -1;
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
}
