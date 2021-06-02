using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Common;
using System.Text.RegularExpressions;

namespace OfflineAudioProcessingSystem.TranscriptValidation
{
    class ValidateAndUpdateFile
    {
        string[] ErrorCodeArray = {
            "Valid",        //0
            "TooShort",     //1
            "NotPair",      //2
            "EmptyLine",    //3
            "TimeStamp",    //4
            "InvalidTag",   //5
            "Digit",        //6
            "InvalidChar",  //7
            "TimeStampReverse",     //8
            "TimeStampMismatch",    //9
            "TimeStampOverlap",     //10
            "TagMismatch",  //11
            "WordCountMismatch",    //12
            "EmptyContent",     //13
            "EmptyMismatch",    //14
        };
        string[] FixArray =
        {
            $"<{LocalConstants.O_Fix}>",
            $"<{LocalConstants.O_Fix}/>",
            $"<{LocalConstants.C_FIX}>",
            $"<{LocalConstants.C_FIX}/>",
        };
        public bool[] ErrorTypeArray { get; private set; }
        (string WrongTag, string CorrectTag)[] ReplaceArray = new (string WrongTag, string NewTag)[0];
        HashSet<string> ValidTagSet = new HashSet<string>();
        public ValidateAndUpdateFile((string WrongTag, string CorrectTag)[] replaceArray, HashSet<string> validTagSet)
        {
            ReplaceArray = replaceArray;
            ValidTagSet = validTagSet;
            ErrorTypeArray = new bool[ErrorCodeArray.Length];
        }

        public string[] ValidateAndCorrectFile(string filePath, string batchName, string taskName, string audioName, string taskId, string audioId, bool ignoreSingle=false,bool ignorePair=false)
        {
            try
            {
                var array = File.ReadAllLines(filePath);
                Sanity.Requires(array.Length >= 10, 1);
                Sanity.Requires(array.Length % 2 == 0, 2);
                for(int i = 0; i < array.Length; i++)
                {
                    string newS = ReorgSingleString(array[i], i, filePath, batchName, taskName, audioName, taskId, audioId);
                    array[i] = newS;
                }
                PreEnd = double.MinValue;

                for (int i = 0; i < array.Length; i += 2)
                {
                    var r = ReorgPairStrings(array[i], array[i + 1], i, filePath, batchName, taskName, audioName, taskId, audioId, ignorePair);
                    array[i] = r.o;
                    array[i + 1] = r.c;
                }
                return array;
            }
            catch(CommonException e)
            {
                string errorMessage = e.HResult == -1 ? e.Message : ErrorCodeArray[e.HResult];
                string toAnnotatorString = string.Join("\t", batchName, taskName, audioName, taskId, audioId, errorMessage, "");
                ToAnnotatorList.Add(toAnnotatorString);
                string errorContent = string.Join("\t", filePath, errorMessage, "");
                ErrorList.Add(errorContent);
                ErrorTypeArray[e.HResult] = true;
                return null;
            }
        }

        public void ValidateTimeStamp(string textPath, string batchName, string taskName, string audioName, string taskId, string audioId)
        {
            string audioPath = textPath.Substring(0,textPath.Length - 3) + "wav";
            Sanity.Requires(File.Exists(audioPath), $"Missing audio:\t{audioPath}");
            string timeStampPath = audioPath + ".timestamp";
            if (!File.Exists(timeStampPath))
                LocalCommon.SetTimeStampsWithVad(audioPath, timeStampPath, 3);

            var collapsedList = IntervalCheck.MergeIntervals(ExtractTimeStampFromAnnotationFile(textPath)).collapsedIntervals;
            var validList = IntervalCheck.MergeIntervals(ExtractTimeStampFromTimeStampFile(timeStampPath)).collapsedIntervals;

            double missed = IntervalCheck.CalculateIntervalMiss(collapsedList, validList);
            TimeStampInfoString=(string.Join("\t", textPath, timeStampPath, batchName, taskName, audioName, taskId, audioId, missed));
        }
        private IEnumerable<(double start, double end)> ExtractTimeStampFromTimeStampFile(string timeStampPath)
        {
            foreach(string s in File.ReadLines(timeStampPath))            
                yield return (double.Parse(s.Split('\t')[0]), double.Parse(s.Split('\t')[1]));            
        }

        private IEnumerable<(double start, double end)> ExtractTimeStampFromAnnotationFile(string annotationPath)
        {
            var array = File.ReadAllLines(annotationPath);
            Sanity.Requires(array.Length % 2 == 0, "Line count is not even.");
            for(int i = 0; i < array.Length; i += 2)
            {
                if (!TimeStampReg.IsMatch(array[i]))
                {
                    Console.WriteLine($"Format error:\t{array[i]}");
                    continue;
                }
                double start = double.Parse(TimeStampReg.Match(array[i]).Groups[1].Value);
                double end = double.Parse(TimeStampReg.Match(array[i]).Groups[2].Value);
                yield return (start, end);
            }
        }

        private Regex TimeStampReg = new Regex("^\\[([0-9.\\-]+)\\s+([0-9.\\-]+)\\]", RegexOptions.Compiled);
        TransLine CurrentLine = new TransLine();
        string CurrentString = "";
        public List<string> ToAnnotatorList = new List<string>();
        public List<string> ErrorList = new List<string>();
        public string TimeStampInfoString { get; set; } = "";
        double PreEnd = double.MinValue;
        TokenMapping Tm = new TokenMapping();
        private string ReorgSingleString(string s, int index, string filePath, string batchName, string taskName, string audioName, string taskId, string audioId)
        {
            try
            {
                CurrentLine = new TransLine();
                CurrentString = s;
                Sanity.Requires(!string.IsNullOrWhiteSpace(s), 3);
                string tail = s.Replace("]", "] ").Replace("<", " <").Replace(">", "> ").Trim();
                tail = CutTimeStamp(s,4);
                tail = CutSpeakerId(tail);
                tail = CutFix(tail, index);
                CurrentLine.Content = CheckContent(tail);
                return CurrentLine.OutputTransLine();
            }
            catch(CommonException e)
            {
                string errorMessage = e.HResult == -1 ? e.Message : ErrorCodeArray[e.HResult];
                string toAnnotatorString = string.Join("\t", batchName, taskName, audioName, taskId, audioId, errorMessage, CurrentString);
                ToAnnotatorList.Add(toAnnotatorString);
                string errorContent = string.Join("\t", filePath, errorMessage, CurrentString);
                ErrorList.Add(errorContent);
                return CurrentString;
            }
        }

        #region Fix Single 
        private string CutTimeStamp(string s, int errorCode)
        {
            var match = TimeStampReg.Match(s);
            Sanity.Requires(match.Success, errorCode);
            string startString = match.Groups[1].Value;
            string endString = match.Groups[2].Value;
            double start, end;
            
            Sanity.Requires(double.TryParse(startString, out start),errorCode);
            Sanity.Requires(double.TryParse(endString, out end), errorCode);
            CurrentLine.StartTime = start;
            CurrentLine.EndTime = end;
            CurrentLine.StartTimeString = startString;
            CurrentLine.EndTimeString = endString;
            if (CurrentLine.StartTime < 0)
            {
                CurrentLine.StartTime = 0;
                CurrentLine.StartTimeString = "0";
            }
            if (CurrentLine.EndTime < 0)
            {
                CurrentLine.EndTime = 0;
                CurrentLine.EndTimeString = "0";
            }
            
            return TimeStampReg.Replace(s, "").Trim();
        }

        private string CutSpeakerId(string s)
        {
            string lower = s.ToLower();
            if (!lower.StartsWith("s1") && !lower.StartsWith("s2"))
            {
                CurrentLine.Speaker = "S1";
                return s;
            }
            CurrentLine.Speaker = s.Substring(0, 2).ToUpper();
            return s.Substring(2).Trim();
        }

        private string CutFix(string s, int index)
        {
            if (index % 2 == 0)
            {
                CurrentLine.Prefix = FixArray[0];
                CurrentLine.Suffix = FixArray[1];
            }
            else
            {
                CurrentLine.Prefix = FixArray[2];
                CurrentLine.Suffix = FixArray[3];
            }
            foreach(string fix in FixArray)
            {
                s = s.Replace(fix, "");
            }
            s = s.Replace(LocalConstants.C_FIX, "").Replace(LocalConstants.O_Fix, "");
            return s.Trim();
        }
        private string CheckContent(string s)
        {
            foreach (var pair in ReplaceArray)
                s = s.Replace(pair.WrongTag, pair.CorrectTag);
            s = s
                .Replace("s1", " ")
                .Replace("S1", " ")
                .Replace("S2", " ")
                .Replace("s2", " ")
                .Replace((char)65311, '?')
                .Replace("?", " <questionmark> ")
                .Replace(":", " ")
                .Replace(",", " <comma> ")
                .Replace(";", " <comma> ")
                .Replace(".", " <fullstop> ")
                .Replace("，", " <comma> ")
                .Replace("·", " <fullstop> ")
                .Replace("。", " <fullstop> ")
                .Replace('`', '\'')
                .Replace("’", "'")
                .Replace("´", "'")
                .Replace('\u00BB', ' ')
                .Replace('\u00AD', ' ')
                .Replace('(', ' ')
                .Replace(')', ' ')
                .Replace('–', ' ')
                .Replace("<<", "<")
                .Replace(">>", ">")
                .Replace(">", "> ")
                .Replace("<", " <")
                .Replace((char)160, ' ')
                .Replace((char)8222, ' ')
                .Replace((char)8221, ' ')
                .Replace((char)8220, ' ')
                .Replace((char)8230, ' ')
                .Replace('\u007f', ' ')
                .Replace('\u001f', ' ')
                .Replace('\u0010', ' ')
                .Replace("&", " und ")
                .Replace("€", " euro ")
                .Replace("+", " plus ")
                .Replace("@", " at ")
                .Replace('#', ' ')
                .Replace('%', ' ')
                .Replace("!\\EXCLAMATION MARK", " !\\EXCLAMATION_MARK ")
                .Replace("_\\Unterstrich", "_\\UNTERSTRICH");
            ;
            if (!s.Contains("!\\"))
                s = s.Replace("!", "<fullstop>");
            if (!s.Contains("\"\\"))
                s = s.Replace('"', ' ');
            string rawS = s;
            foreach (string validTag in ValidTagSet)
                rawS = rawS.Replace(validTag, "");
            rawS = rawS
                .Replace("-\\BINDESTRICH", "")
                .Replace("!\\AUSRUFEZEICHEN", "")
                .Replace("\"\\ANFÜHRUNGSZEICHEN", "")
                .Replace("!\\EXCLAMATION_MARK", "")
                .Replace("_\\UNTERSTRICH", "");


            foreach (char c in rawS)
            {
                if (c >= 'A' && c <= 'Z')
                    continue;
                if (c >= 'a' && c <= 'z')
                    continue;
                if (c >= '\u00c0' && c <= '\u017f')
                    continue;
                if (c == ' ' || c == '-' ||c=='\'' || c == 946)
                    continue;
                if (c == '<' || c == '>')
                    throw new CommonException(5);
                if (c >= '0' && c <= '9')
                    throw new CommonException(6);
                throw new CommonException(7);
            }
            return s.CleanupSpace();
        }
        #endregion

        private (string o, string c) ReorgPairStrings(string o, string c, int index, string filePath, string batchName, string taskName, string audioName, string taskId, string audioId, bool ignoreAll=false)
        {
            try
            {
                var r = (o, c);
                if (!ignoreAll)
                {
                    ValidateEmpty(o, c);
                    ValidateWordCount(o, c);
                    r = ValidateTimeStamp(o, c);
                    r = ValidateTags(r.o, r.c);
                }
                return r;
            }
            catch(CommonException e)
            {
                string errorMessage = e.HResult == -1 ? e.Message : ErrorCodeArray[e.HResult];
                string toAnnotatorString1 = string.Join("\t", batchName, taskName, audioName, taskId, audioId, errorMessage, o);
                string toAnnotatorString2 = string.Join("\t", batchName, taskName, audioName, taskId, audioId, errorMessage, c);
                ToAnnotatorList.Add(toAnnotatorString1);
                ToAnnotatorList.Add(toAnnotatorString2);
                string errorContent1 = string.Join("\t", filePath, errorMessage, o);
                string errorContent2 = string.Join("\t", filePath, errorMessage, c);
                ErrorList.Add(errorContent1);
                ErrorList.Add(errorContent2);
                return (o, c);
            }
        }
       
        #region Fix Pairs
        private (string o,string c) ValidateTimeStamp(string oString, string cString)
        {
            if (!LocalCommon.TransLineRegex.IsMatch(oString) || !LocalCommon.TransLineRegex.IsMatch(cString))
                return (oString, cString);
            var oLine = LocalCommon.ExtractTransLine(oString);
            var cLine = LocalCommon.ExtractTransLine(cString);
            double preEnd = PreEnd;
            PreEnd = oLine.EndTime;
            Sanity.Requires(oLine.EndTime > oLine.StartTime, 8);
            Sanity.Requires(Math.Abs(oLine.EndTime - cLine.EndTime) <= 0.1, 9);
            Sanity.Requires(Math.Abs(oLine.StartTime - cLine.StartTime) <= 0.1, 9);
            Sanity.Requires(oLine.StartTime - preEnd >= -0.5, 10);
            cLine.StartTimeString = oLine.StartTimeString;
            cLine.EndTimeString = cLine.EndTimeString;
            cLine.StartTime = oLine.StartTime;
            cLine.EndTime = oLine.EndTime;
            return (oLine.OutputTransLine(), cLine.OutputTransLine());
        }
        private void ValidateEmpty(string oString,string cString)
        {
            var oLine = LocalCommon.ExtractTransLine(oString);
            var cLine = LocalCommon.ExtractTransLine(cString);
            if (string.IsNullOrWhiteSpace(oLine.Content) && string.IsNullOrWhiteSpace(cLine.Content))
                Sanity.Throw(13);
            if (string.IsNullOrWhiteSpace(oLine.Content) || string.IsNullOrWhiteSpace(cLine.Content))
                Sanity.Throw(14);
        }
        private (string o, string c)ValidateTags(string oString, string cString)
        {
            if (!LocalCommon.TransLineRegex.IsMatch(oString) || !LocalCommon.TransLineRegex.IsMatch(cString))
                return (oString, cString);
            var oLine = LocalCommon.ExtractTransLine(oString);
            var cLine = LocalCommon.ExtractTransLine(cString);
            var r = Tm.MappingTokens(oLine.Content, cLine.Content, 11);
            oLine.Content = r.oContent;
            cLine.Content = r.cContent;
            return (oLine.OutputTransLine(), cLine.OutputTransLine());
        }

        private void ValidateWordCount(string oString,string cString)
        {
            if (!LocalCommon.TransLineRegex.IsMatch(oString) || !LocalCommon.TransLineRegex.IsMatch(cString))
                return;
            var oLine = LocalCommon.ExtractTransLine(oString);
            var cLine = LocalCommon.ExtractTransLine(cString);
            var oTokens = oLine.Content.Split(LocalCommon.Sep, StringSplitOptions.RemoveEmptyEntries).Where(x => x[0] != '<');
            var cTokens = cLine.Content.Split(LocalCommon.Sep, StringSplitOptions.RemoveEmptyEntries).Where(x => x[0] != '<');
            int oLength = oTokens.Count();
            int cLength = cTokens.Count();
            Sanity.Requires(LengthClose(oLength, cLength) && LengthClose(cLength, oLength), 12);            
        }

        private static bool LengthClose(int big, int small)
        {
            switch (small)
            {
                case 0:
                    return big == 0;
                case 1:
                    return big <= 3;
                case 2:
                case 3:
                case 4:
                    return big <= 2*small;
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    return big <= 1.5 * small;
                default:
                    return big <= 1.3 * small || big <= 5 + small;
            }
        }
        #endregion

        #region Offline data to input

        #endregion
    }

    class TextGridParser
    {
        string[] ErrorArray =
        {
            "Succeed",      // 0
            "LayerCountError",  // 1
            "LayerNameError",   // 2
            "IntervalCountMismatch",    // 3
            "IntervalsTooShort",        // 4
            "IntervalMismatch",     //5
            "Empty file",       //6
        };
        public TextGridParser() { }
        public List<string> ErrorList { get; set; } = new List<string>();
        Regex InItemReg = new Regex("^\\s*item\\s*\\[([0-9]+)\\]:\\s*$", RegexOptions.Compiled);
        Regex NameReg = new Regex("^\\s*name\\s*=\\s*\"(.*)\"\\s*$", RegexOptions.Compiled);
        Regex IntervalStartReg = new Regex("^\\s*intervals\\s*\\[([0-9]+)\\]:\\s*$", RegexOptions.Compiled);
        Regex XMinReg = new Regex("^\\s*xmin\\s*=\\s*([0-9.\\-]+)\\s*$", RegexOptions.Compiled);
        Regex XMaxReg = new Regex("^\\s*xmax\\s*=\\s*([0-9.\\-]+)\\s*$", RegexOptions.Compiled);
        Regex TextReg = new Regex("^\\s*text\\s*=\\s*\"(.*)\"\\s*$", RegexOptions.Compiled);
        Regex TwoSpReg = new Regex("[0-9]{5}_[0-9]{5}", RegexOptions.Compiled);
        public void ExtractTextGridFile(string textgridPath, bool overwrite=false)
        {
            string outputPath = textgridPath.Substring(0, textgridPath.Length - 8) + "txt";
            if (File.Exists(outputPath))
            {
                string fileName = textgridPath.Split('\\').Reverse().ElementAt(1);
                if (!TwoSpReg.IsMatch(fileName))
                    return;
            }
            try
            {
                var list = File.ReadLines(textgridPath);
                var intervalSeq = ExtractToIntervals(list);
                var dict = PostVerifyTextgrid(intervalSeq);
                if (dict.Count != 2)
                {
                    if (dict.Count == 3 && dict.ContainsKey("SG") && dict.ContainsKey("HG") && dict.ContainsKey("SP"))
                        ;
                    else
                    {
                        Console.WriteLine(textgridPath);
                        return;
                    }
                }
                CheckIntervalsTwo(dict["SG"], dict["HG"]);
                if (dict.ContainsKey("SP"))
                    CheckIntervalsTwo(dict["SG"], dict["SP"]);
                string sgPath = textgridPath + ".sg";
                string hgPath = textgridPath + ".hg";
                string speakerPath = textgridPath + ".sp";
                OutputSplitTextGrid(dict, sgPath, hgPath, speakerPath, overwrite);
                if (!File.Exists(speakerPath))
                    MergeSgHg(sgPath, hgPath, outputPath);
                else
                    MergeSgHgSp(sgPath, hgPath, speakerPath, outputPath);
            }
            catch(CommonException e)
            {
                string errorType = "Others";
                if (e.HResult != -1)
                    errorType = ErrorArray[e.HResult];
                ErrorList.Add(string.Join("\t", textgridPath, errorType, e.Message));
            }
        }
        private void MergeSgHg(string sgPath, string hgPath, string outputPath)
        {
            if (!File.Exists(sgPath) || !File.Exists(hgPath))
                return;
            var sg = File.ReadAllLines(sgPath);
            var hg = File.ReadAllLines(hgPath);
            Sanity.Requires(sg.Length == hg.Length, "NO REPORT", 5);
            Sanity.Requires(sg.Length != 0, "NO REPORT", 6);
            List<string> list = new List<string>();
            for (int i = 0; i < sg.Length; i++)
            {
                list.Add(sg[i]);
                list.Add(hg[i]);
            }
            File.WriteAllLines(outputPath, list);
        }
        private void MergeSgHgSp(string sgPath, string hgPath, string spPath, string outputPath)
        {
            if (!File.Exists(sgPath) || !File.Exists(hgPath) || !File.Exists(spPath))
                return;
            var sg = File.ReadAllLines(sgPath);
            var hg = File.ReadAllLines(hgPath);
            var sp = File.ReadAllLines(spPath);
            Sanity.Requires(sg.Length == hg.Length && hg.Length == sp.Length, "NO REPORT", 5);
            Sanity.Requires(sg.Length != 0, "NO REPORT", 6);
            List<string> list = new List<string>();
            for(int i = 0; i < sg.Length; i++)
            {
                var sgLine = LocalCommon.ExtractTransLine(sg[i]);
                var hgLine = LocalCommon.ExtractTransLine(hg[i]);
                var spLine = LocalCommon.ExtractTransLine(sp[i]);
                sgLine.Speaker = $"S{spLine.Content.Trim()}";
                hgLine.Speaker = $"S{spLine.Content.Trim()}";
                list.Add(sgLine.OutputTransLine());
                list.Add(hgLine.OutputTransLine());
            }
            File.WriteAllLines(outputPath, list);
        }
        private void OutputSplitTextGrid(Dictionary<string, Interval[]> dict, string sgPath, string hgPath, string speakerPath="", bool overwrite = false)
        {
            var sgList = dict["SG"].Where(x => !string.IsNullOrWhiteSpace(x.Text)).Select(x => GetTransLine(x, "SG").OutputTransLine());
            var hgList = dict["HG"].Where(x => !string.IsNullOrWhiteSpace(x.Text)).Select(x => GetTransLine(x, "HG").OutputTransLine());

            var spList = dict.ContainsKey("SP") ? dict["SP"].Where(x => !string.IsNullOrWhiteSpace(x.Text)).Select(x => GetTransLine(x, "SP").OutputTransLine()) : null;
            if (overwrite || !File.Exists(sgPath))
                File.WriteAllLines(sgPath, sgList);
            if (overwrite || !File.Exists(hgPath))
                File.WriteAllLines(hgPath, hgList);
            if (overwrite || !File.Exists(speakerPath))
                if (spList != null)
                    File.WriteAllLines(speakerPath, spList);
        }
        private TransLine GetTransLine(Interval interval, string name)
        {
            string fix = name == "SG" ? LocalConstants.O_Fix : LocalConstants.C_FIX;
            string speaker = "S1";
            if (interval.Text.ToUpper().StartsWith("S1"))
            {
                speaker = "S1";
                interval.Text = interval.Text.Substring(2);
            }
            if (interval.Text.ToUpper().StartsWith("S2"))
            {
                speaker = "S2";
                interval.Text = interval.Text.Substring(2);
            }
            return new TransLine
            {
                Prefix = $"<{fix}>",
                Suffix = $"<{fix}/>",
                Content = interval.Text,
                Speaker = speaker,
                StartTimeString = interval.XMinString,
                StartTime = double.Parse(interval.XMinString),
                EndTimeString = interval.XMaxString,
                EndTime = double.Parse(interval.XMaxString)
            };
        }
        private Dictionary<string,Interval[]> PostVerifyTextgrid(IEnumerable<Interval> intervalSeq)
        {
            var dict = intervalSeq.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.ToArray());
            int g = dict.Count();
            var groupNames = dict.Select(x => x.Key).ToArray();
            Sanity.Requires(g == 2 || g == 3, $"Layer count: {dict.Count}", 1);
            Sanity.Requires(dict.ContainsKey("SG") && dict.ContainsKey("HG"), string.Join(" ", dict.Keys), 2);
            int max = dict.Max(x => x.Value.Length);
            int min = dict.Min(x => x.Value.Length);
            Sanity.Requires(min + 5 >= max, $"Min: {min}, Max:{max}", 3);
            Sanity.Requires(min * 1.1 >= max, $"Min: {min}, Max:{max}", 3);
            Sanity.Requires(min >= 10, $"Interval count: {min}", 4);
            return dict;
        }
        private void CheckIntervalsTwo(Interval[] sgArray, Interval[] hgArray)
        {
            var nonEmptySg = sgArray.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToArray();
            var nonEmptyHg = hgArray.Where(x => !string.IsNullOrWhiteSpace(x.Text)).ToArray();
            int sgIndex = 0;
            int hgIndex = 0;
            while (sgIndex < nonEmptySg.Length && hgIndex < nonEmptyHg.Length)
            {
                var currentSg = nonEmptySg[sgIndex];
                var currentHg = nonEmptyHg[hgIndex];
                if (IntervalMatch(currentSg, currentHg)||IntervalValueMatch(currentSg,currentHg))
                {
                    sgIndex++;
                    hgIndex++;
                    continue;
                }                
                Sanity.Throw($"{currentSg.XMinString} {currentSg.XMaxString}", 5);
            }
            if (sgIndex == nonEmptySg.Length && hgIndex == nonEmptyHg.Length)
                return;
            Sanity.Throw(
                sgIndex<nonEmptySg.Length
                ?$"{nonEmptySg[sgIndex].XMinString} {nonEmptySg[sgIndex].XMaxString}"
                :$"{nonEmptyHg[hgIndex].XMinString} {nonEmptyHg[hgIndex].XMaxString}"
                , 5);
        }
        private bool IntervalMatch(Interval sg, Interval hg)
        {
            string sgKey1 = GetBigDoubleKey(sg.XMinString);
            string sgKey2 = GetBigDoubleKey(sg.XMaxString);
            string hgKey1 = GetBigDoubleKey(hg.XMinString);
            string hgKey2 = GetBigDoubleKey(hg.XMaxString);
            return sgKey1 == hgKey1 && sgKey2 == hgKey2;
        }

        private bool IntervalValueMatch(Interval sg, Interval hg)
        {
            double diff1 = Math.Abs(double.Parse(sg.XMinString) - double.Parse(hg.XMinString));
            double diff2 = Math.Abs(double.Parse(sg.XMaxString) - double.Parse(hg.XMaxString));
            return diff1 <= 0.1 && diff2 <= 0.1;
        }

        private string GetBigDoubleKey(string s)
        {
            int length = Math.Min(s.Length, 5);
            return s.Substring(0, length);
        }
        private IEnumerable<Interval> ExtractToIntervals(IEnumerable<string> textgridSeq)
        {
            string currentName = null;
            Interval interval = new Interval() { IntervalId = -1 };
            int previous = 0;
            bool inInterval = false;
            List<string> textList = new List<string>();
            foreach(string s in textgridSeq)
            {
                if (InItemReg.IsMatch(s))
                {
                    if (interval.IntervalId != -1)
                    {
                        interval = CreateInterval(interval, textList, currentName);
                        yield return interval;
                    }
                    interval = new Interval { IntervalId = -1 };
                    previous = 0;
                    inInterval = false;
                    currentName = null;
                    continue;
                }
                if (NameReg.IsMatch(s))
                {
                    currentName = NameReg.Match(s).Groups[1].Value.ToUpper();
                    Sanity.Requires(previous == 0);
                    inInterval = false;
                    continue;
                }
                if (IntervalStartReg.IsMatch(s))
                {
                    inInterval = true;
                    if (interval.IntervalId != -1)
                    {
                        interval = CreateInterval(interval, textList, currentName);
                        yield return interval;
                    }
                    int index = int.Parse(IntervalStartReg.Match(s).Groups[1].Value);
                    Sanity.Requires(index == previous + 1);
                    textList = new List<string>();
                    interval.XMinString = null;
                    interval.XMaxString = null;
                    interval.Text = null;
                    interval = new Interval { IntervalId = index };
                    previous = index;
                    continue;
                }
                if (XMinReg.IsMatch(s))
                {
                    if (!inInterval)
                        continue;
                    Sanity.Requires(interval.IntervalId != -1);
                    Sanity.Requires(interval.XMinString == null);
                    interval.XMinString = XMinReg.Match(s).Groups[1].Value;
                    continue;
                }
                if (XMaxReg.IsMatch(s))
                {
                    if (!inInterval)
                        continue;
                    Sanity.Requires(interval.IntervalId != -1);
                    Sanity.Requires(interval.XMaxString == null);
                    interval.XMaxString = XMaxReg.Match(s).Groups[1].Value;
                    continue;
                }
                if (inInterval)
                {
                    Sanity.Requires(interval.IntervalId != -1);
                    Sanity.Requires(interval.XMinString != null);
                    Sanity.Requires(interval.XMaxString != null);
                    textList.Add(s);
                    continue;
                }
            }
            interval = CreateInterval(interval, textList, currentName);
            yield return interval;
        }

        private Interval CreateInterval(Interval interval, List<string> textList, string currentName)
        {
            Sanity.Requires(currentName != null);
            interval.Name = currentName;
            Sanity.Requires(interval.IntervalId > 0);
            Sanity.Requires(interval.XMaxString != null);
            Sanity.Requires(interval.XMinString != null);
            Sanity.Requires(interval.Text == null);
            Sanity.Requires(textList.Count >= 1);
            string text = string.Join(" ", textList);
            Sanity.Requires(TextReg.IsMatch(text));
            interval.Text = TextReg.Match(text).Groups[1].Value.Trim();
            return interval;
        }
    }
    struct Interval
    {
        public string Name { get; set; }
        public string XMinString { get; set; }
        public string XMaxString { get; set; }
        public string Text { get; set; }
        public int IntervalId { get; set; }
    }
}
