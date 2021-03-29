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
        public string MissingPath { get; set; } = "";
        public string AudioTimePath { get; set; } = "";
        public string InputAudioTimePath { get; set; } = "";
        const int MAX_SHOW_COUNT = 50;
        List<string> FullList = new List<string>();
        //List<string> PostCheckList = new List<string>();
        List<string> InputAudioList = new List<string>();
        public TranscriptValidation()
        {
        }
        public void Test()
        {
            string s = "[5 9.75]S1 <chdialects-converted> Guten Tag <comma> ist <UNKNOWN> Ihr Vater <questionmark><chdialects-converted> ";
            //var r = ExtractLine(s, 0);
        }
        public void RunValidation(string specificPath="",bool ignoreDialectTag=false)
        {
            Init();
            ValidateFolder(InputRootPath, specificPath, ignoreDialectTag);            
            File.WriteAllLines(AllPath, FullList);
            File.WriteAllLines(InputAudioTimePath, InputAudioList);
        }
        public void RunUpdate(Dictionary<string,AnnotationLine> set, bool create = false)
        {
            Init();
            //UpdateFolder(InputRootPath, OutputRootPath, set, create);
            UpdateFolderNew(InputRootPath, set, OutputRootPath, create);
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
        private void ValidateFolder(string inputRootPath, string specificPath="", bool ignoreDialectTag=false)
        {            
            foreach (string filePath in Directory.EnumerateFiles(inputRootPath, "*.txt", SearchOption.AllDirectories))
            {
                string overallAudioPath;
                string inputAudioPath1 = filePath.Substring(0, filePath.Length - 3) + "wav";
                string inputAudioPath2 = filePath.Replace(".txt", ".wav");
                if (File.Exists(inputAudioPath1))
                    overallAudioPath = inputAudioPath1;
                else if (File.Exists(inputAudioPath2))
                    overallAudioPath = inputAudioPath2;
                else
                    throw new CommonException("Audio missing.");                
                Wave w = new Wave();
                w.ShallowParse(overallAudioPath);
                InputAudioList.Add($"{overallAudioPath}\t{w.AudioLength}"); ;
                ValidateFile(filePath, specificPath,ignoreDialectTag);
                ValidateFileAll(filePath,specificPath,ignoreDialectTag);
            }
        }
        public void SetDialectTag(string path)
        {
            var list = File.ReadAllLines(path);
            Sanity.Requires(list.Length % 2 == 0);
            for(int i = 0; i < list.Length; i += 2)
            {
                list[i] = list[i].Replace("]", $"]{O_PREFIX}") + O_SUFFIX;
                list[i + 1] = list[i + 1].Replace("]", $"]{C_PREFIX}") + C_SUFFIX;
            }
            string outputPath = path + ".update";
            File.WriteAllLines(outputPath, list);
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
                        validKeySet.Remove(key);
                        validKeySet.Remove(alterKey);
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
            File.WriteAllLines(MissingPath, validKeySet);
            File.WriteAllLines(MappingPath, mappingList);
        }
        public void UpdateFolderNew(string inputRootPath, Dictionary<string, AnnotationLine> validKeySet, string outputRootPath, bool create=false)
        {
            List<string> mappingList = new List<string>();
            double totalTime = 0;
            double validTime = 0;
            int m1 = 0;
            int m2 = 0;
            foreach (string taskFolderPath in Directory.EnumerateDirectories(inputRootPath))
            {
                string taskName = taskFolderPath.Split('\\').Last();
                string taskId = taskName.Split('_')[0];
                string speakerFolder = Path.Combine(taskFolderPath, "Speaker");
                string locale = GetLocale(taskFolderPath);
                string speakerId = taskName.Split('_').Last();
                if (!(speakerId.Length == 5 && speakerId.All(x => x >= '0' && x <= '9')))
                    speakerId = "";
                string outputSpeakerPath = Path.Combine(outputRootPath, taskName,"Speaker");
                Directory.CreateDirectory(outputSpeakerPath);
                foreach (string textPath in Directory.EnumerateFiles(speakerFolder, "*.txt"))
                {
                    string audioName = textPath.Split('\\').Last().Replace(".txt", ".wav");
                    string alterName = audioName.Replace("ü", "u_").Replace(".txt", ".wav");
                    string key = $"{taskId}\t{audioName}";
                    string alterKey = $"{taskId}\t{alterName}";
                    if (validKeySet == null||validKeySet.Keys.Contains(key) || validKeySet.Keys.Contains(alterKey))
                    {
                        if (!ValidateFileAll(textPath))
                            continue;
                        var list = ValidateFile(textPath);
                        string audioPath = Path.Combine(speakerFolder, audioName);
                        Wave w = new Wave();
                        w.ShallowParse(audioPath);
                        double t = w.AudioLength;
                        totalTime += t;
                        m1++;
                        if (list != null && list.Count > 0&&list.Count==File.ReadLines(textPath).Count())
                        {
                            string fileName = textPath.Split('\\').Last();
                            var outputList = list.Select(x => OutputTransLine(x));
                            string outputTextPath = Path.Combine(outputSpeakerPath, fileName);
                            File.WriteAllLines(outputTextPath, outputList);
                            ValidateFileAll(outputTextPath);
                            if (validKeySet != null)
                            {
                                validKeySet.Remove(key);
                                validKeySet.Remove(alterKey);
                            }
                            if (create)
                            {
                                validTime += t;
                                m2++;
                                string outputAudioPath = Path.Combine(outputSpeakerPath, audioName);
                                File.Copy(audioPath, outputAudioPath, true);
                            }
                        }
                    }
                }
            }
            List<string> audioTimeList = new List<string>
            {
                $"{validTime}\t{totalTime}",
                $"{validTime/3600}\t{totalTime/3600}"
            };
            File.WriteAllLines(AllPath, FullList);
            
            File.WriteAllLines(MappingPath, mappingList);
            File.WriteAllLines(AudioTimePath, audioTimeList);
            if (validKeySet != null)
                File.WriteAllLines(MissingPath, validKeySet.Keys);
        }
        public void MergeTextGrid(string textGridFolder, string audioFolder, string outputFolder, string reportPath, bool useExistingSgHg)
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
                    string fileName = tgFilePath.GetFileName().Item1;
                    string sgPath = Path.Combine(outputTaskFolder, fileName + ".txt.sg");
                    string hgPath = Path.Combine(outputTaskFolder, fileName + ".txt.hg");
                    try
                    {
                        string wavePath = Path.Combine(audioTaskFolder, fileName + ".wav");
                        string outputTextFilePath = Path.Combine(outputTaskFolder, fileName + ".txt");
                        string outputWaveFilePath = Path.Combine(outputTaskFolder, fileName + ".wav");
                        TextGrid.TextGridToText(tgFilePath, outputTextFilePath, sgPath,hgPath, useExistingSgHg);
                        string outputWavePath = Path.Combine(outputTaskFolder, fileName + ".wav");
                        if (!TextGrid.Reject)
                        {
                            Sanity.Requires(File.Exists(wavePath));
                            File.Copy(wavePath, outputWavePath, true);
                        }
                        else
                        {
                            reportList.AddRange(TextGrid.AllList.Select(x => $"{tgFilePath}\t{x}"));
                        }
                    }catch(CommonException ce)
                    {
                        string s1 = $"{sgPath}\t{ce.Message}";
                        string s2 = $"{hgPath}\t{ce.Message}";
                        Console.WriteLine(s1);
                        Console.WriteLine(s2);
                        reportList.Add(s1);
                        reportList.Add(s2);
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
            "Empty content",    //  16
            "Tag mismatch",     //  17
            "Word count mismatch",      //  18
            "Too many UNK",     //  19            
        };

        string TimeStampString = "";
        private bool ValidateFileAll(string filePath, string specificPath="", bool ignoreDialectTag=false, bool addDialectTag=false)
        {
            if (filePath == specificPath)
                ;
            var r = File.ReadLines(filePath).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            string taskNameContent = GetTaskFormat(filePath);
            List<TransLine> list = new List<TransLine>();
            bool valid = true;
            try
            {
                Sanity.Requires(r.Length > 2, filePath, 1);
                int i = 0;
                foreach (string s in r)
                {
                    TransLine line = new TransLine();
                    TimeStampString = "";
                    
                    try
                    {
                        line = ExtractLine(s,i,ignoreDialectTag);
                        i++;
                        line.Content = ValidateContent(line.Content, i, ignoreDialectTag);
                        list.Add(line);
                    }
                    catch(CommonException e)
                    {
                        string content = $"{filePath}\t{taskNameContent}\t{TransErrorArray[e.HResult]}\t{s}";                        
                        FullList.Add(content);
                        valid = false;
                    }
                }
            }
            catch(CommonException e)
            {
                string content = $"{filePath}\t{taskNameContent}\t{TransErrorArray[e.HResult]}";
                FullList.Add(content);
                valid = false;
            }
            if (valid)
                valid = ValidateTransLineList(list, filePath, taskNameContent);
            return valid;
        }
        private List<TransLine> ValidateFile(string filePath, string specificPath = "", bool ignoreDialectTag = false, bool addDialectTag = false)
        {
            if (filePath == specificPath)
                ;
            var r = File.ReadLines(filePath).Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();
            List<TransLine> list = new List<TransLine>();
            try
            {
                Sanity.Requires(r.Length > 2, filePath, 1);
                int i = 0;
                foreach (string s in r)
                {
                    try
                    {
                        var line = ExtractLine(s,i,ignoreDialectTag);
                        i++;
                        line.Content = ValidateContent(line.Content,i, ignoreDialectTag);
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
                    case 16:    //Empty content
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

        private bool ValidateTransLineList(List<TransLine> list, string filePath, string taskNameContent)
        {
            int unkCount = 0;
            for(int i = 0; i < list.Count; i+=2)
            {
                string so = "";
                string sc = "";
                try
                {
                    int u;
                    var oLine = list[i];
                    so = OutputTransLine(oLine);
                    var cLine = list[i + 1];
                    sc = OutputTransLine(cLine);
                    ValidateTransLinePair(oLine, cLine, out u);
                    unkCount += u;
                    
                }
                catch(CommonException e)
                {
                    string contentO = $"{filePath}\t{taskNameContent}\t{TransErrorArray[e.HResult]}\t{so}";
                    string contentC = $"{filePath}\t{taskNameContent}\t{TransErrorArray[e.HResult]}\t{sc}";
                    FullList.Add(contentO);
                    FullList.Add(contentC);
                    return false;
                }
            }
            try
            {
                Sanity.Requires(unkCount <= 5, 19);
            }
            catch(CommonException e)
            {
                string content = $"{filePath}\t{taskNameContent}\t{TransErrorArray[e.HResult]}\t";
                FullList.Add(content);
                return false;
            }
            return true;
        }

        private void ValidateTransLinePair(TransLine oLine, TransLine cLine, out int unkCount)
        {
            Sanity.Requires(oLine.StartTime == cLine.StartTime, 9);
            Sanity.Requires(oLine.EndTime == cLine.EndTime, 9);
            Sanity.Requires(oLine.StartTime < oLine.EndTime, 9);


            var oTags = TagReg.Matches(oLine.Content).Cast<Match>().Select(x => x.Value).ToArray();
            var cTags = TagReg.Matches(cLine.Content).Cast<Match>().Select(x => x.Value).ToArray();
            Sanity.Requires(oTags.SequenceEqual(cTags), 17);

            int oCount = oLine.Content.Split(' ').Length;
            int cCount = cLine.Content.Split(' ').Length;

            Sanity.Requires(oCount * 1.3 >= cCount || oCount + 1 >= cCount, 18);
            Sanity.Requires(cCount * 1.3 >= oCount || cCount + 1 >= oCount, 18);


            unkCount = oTags.Where(x => x == "<UNKNOWN/>").Count();
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

        private TransLine ExtractLine(string s, int n, bool ignoreDialectTag=false)
        {
            s = SpaceReg.Replace(s, " ").Trim();
            TransLine line = new TransLine();
            line = SetTimeStamp(s, line);
            line = SetSpeakerId(line.Content, line);
            if (!ignoreDialectTag)
                line = SetDialectFix(line.Content, line, n, ignoreDialectTag);
            Sanity.Requires(!string.IsNullOrWhiteSpace(line.Content), line.StartTime.ToString(), 16);
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
            if (!lower.Contains("s1") && !lower.Contains("s2"))
            {
                line.Speaker = "S1";
                line.Content = s;
                return line;
            }
            Sanity.Requires(lower.Contains("s1") || lower.Contains("s2"), line.StartTime.ToString(), 15);
            Sanity.Requires(lower.StartsWith("s1") || lower.StartsWith("s2"), line.StartTime.ToString(), 4);
            line.Speaker = s.Substring(0, 2).ToUpper();
            line.Content = s.Substring(2).Trim();
            return line;
        }
        const string O_PREFIX = "<chdialects>";
        const string O_SUFFIX = "<chdialects/>";
        const string C_PREFIX = "<chdialects-converted>";
        const string C_SUFFIX = "<chdialects-converted/>";
        private TransLine SetDialectFix(string s, TransLine line, int n,bool ignoreDialect)
        {
            s = s.Trim();            
            if (s.Trim().ToLower() == "<unknown/>")
                return line;
            string subContent = s.Substring(0, Math.Min(MAX_SHOW_COUNT, s.Length));
            if (s.StartsWith(O_PREFIX) && s.EndsWith(O_SUFFIX))
                return GetMiddle(s, O_PREFIX, O_SUFFIX, line);
            if (s.StartsWith(C_PREFIX) && s.EndsWith(C_SUFFIX))
                return GetMiddle(s, C_PREFIX, C_SUFFIX, line);
            if (s.StartsWith(O_PREFIX))
            {
                if (!s.EndsWith(O_SUFFIX))
                    return SetDialectFix($"{s}{O_SUFFIX}", line,n, ignoreDialect);
                string middle = s.Replace(O_PREFIX, "").Trim();
                line.Prefix = O_PREFIX;
                line.Content = middle;
                line.Suffix = O_SUFFIX;
                return line;                
            }
            if (s.StartsWith(C_PREFIX))
            {
                if (!s.EndsWith(C_SUFFIX))
                    return SetDialectFix($"{s}{C_SUFFIX}", line,n, ignoreDialect);
                string middle = s.Replace(C_PREFIX, "").Trim();
                line.Prefix = C_PREFIX;
                line.Content = middle;
                line.Suffix = C_SUFFIX;
                return line;                
            }
            //if(n%2==0)
            //{
            //    line.Prefix = O_PREFIX;
            //    line.Suffix = O_SUFFIX;
            //    return line;
            //}
            //else
            //{
            //    line.Prefix = C_PREFIX;
            //    line.Suffix = C_SUFFIX;
            //    return line;
            //}
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
                        
        private string ValidateContent(string content, int n, bool ignoreDialectTag=false)
        {
            string subContent = content.Substring(0, Math.Min(content.Length, MAX_SHOW_COUNT));
            content = content
                .Replace("S1", " ")
                .Replace("S2", " ");
            Sanity.Requires(!content.ToLower().Contains("s1") && !content.ToLower().Contains("s2"), subContent, 10);
            if (content.ToLower().Contains("chdialects"))
                ;
            if (!ignoreDialectTag)
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
                .Replace("´", "'")
                .Replace("，", "<comma>")
                .Replace("。", "<fullstop>")
                .Replace("\"", " ")
                .Replace("<unknown>","<UNKNOWN/>")
                .Replace('\u00BB',' ')
                .Replace('\u00AD',' ')
                .Replace('(', ' ')
                .Replace(')',' ')
                .Replace('–',' ')
                .Replace("<<","<")
                .Replace(">>",">")
                .Replace("<comma/>","<comma>")
                .Replace("<question_mark/>","<questionmark>")
                .Replace("<fullstop/>","<fullstop>")
                .Replace("<fill/>", "<FILL/>")
                .Replace("<cnoise>","<CNOISE/>")
                .Replace("<CNOISE>", "<CNOISE/>")
                .Replace("<exclamation_mark/>", "<fullstop>")
                ;
            if (!content.Contains("!\\"))
                content = content.Replace("!", "<fullstop>");
            

            var localTags = GetTags(content).ToArray();
            var diff = localTags.Except(ValidTagSet);
            Sanity.Requires(diff.Count() == 0, subContent, 12);
            TagSet.UnionWith(localTags);
            string rawContent = TagReg.Replace(content, " ")
                .Replace("-\\BINDESTRICH", "")
                .Replace("!\\AUSRUFEZEICHEN", "")
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
                if (c == ' ' || c == '-' || c == '\''||c==946)
                    continue;
                if (c >= '0' && c <= '9')
                    throw new CommonException($"{c}", 13);
                throw new CommonException($"{c}", 14);
            }
            CharSet.UnionWith(rawContent);
            content = CleanupSpace(content);
            Sanity.Requires(!string.IsNullOrWhiteSpace(content), subContent, 16);
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
