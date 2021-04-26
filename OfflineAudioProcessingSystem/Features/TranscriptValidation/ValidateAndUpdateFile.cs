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
        };
        string[] FixArray =
        {
            $"<{LocalConstants.O_Fix}>",
            $"<{LocalConstants.O_Fix}/>",
            $"<{LocalConstants.C_FIX}>",
            $"<{LocalConstants.C_FIX}/>",
        };
        (string WrongTag, string CorrectTag)[] ReplaceArray = new (string WrongTag, string NewTag)[0];
        HashSet<string> ValidTagSet = new HashSet<string>();
        public ValidateAndUpdateFile((string WrongTag, string CorrectTag)[] replaceArray, HashSet<string> validTagSet)
        {
            ReplaceArray = replaceArray;
            ValidTagSet = validTagSet;
        }

        public void ValidateAndCorrectFile(string filePath, string batchName, string taskName, string audioName, string taskId, string audioId)
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
                    var r = ReorgPairStrings(array[i], array[i + 1], i, filePath, batchName, taskName, audioName, taskId, audioId);
                    array[i] = r.o;
                    array[i + 1] = r.c;
                }
            }
            catch(CommonException e)
            {
                string errorMessage = e.HResult == -1 ? e.Message : ErrorCodeArray[e.HResult];
                string toAnnotatorString = string.Join("\t", batchName, taskName, audioName, taskId, audioId, errorMessage, "");
                ToAnnotatorList.Add(toAnnotatorString);
                string errorContent = string.Join("\t", filePath, errorMessage, "");
                ErrorList.Add(errorContent);
            }
        }
        private Regex TimeStampReg = new Regex("^\\[([0-9.\\-]+)\\s+([0-9.\\-]+)\\]", RegexOptions.Compiled);
        TransLine CurrentLine = new TransLine();
        string CurrentString = "";
        public List<string> ToAnnotatorList = new List<string>();
        public List<string> ErrorList = new List<string>();
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
            s=s
                .Replace("s1","")
                .Replace("S1","")
                .Replace("S2","")
                .Replace("s2","")
                .Replace(">","> ")
                .Replace("<"," <")
                .Replace("?", "<questionmark>")
                .Replace(":", "<comma>")
                .Replace(",", "<comma>")
                .Replace(";", "<comma>")
                .Replace(".", "<fullstop>")
                .Replace("，", "<comma>")
                .Replace("。", "<fullstop>")
                .Replace('`', '\'')
                .Replace("’", "'")
                .Replace("´", "'")
                .Replace("\"", " ")
                .Replace('\u00BB', ' ')
                .Replace('\u00AD', ' ')
                .Replace('(', ' ')
                .Replace(')', ' ')
                .Replace('–', ' ')
                .Replace("<<", "<")
                .Replace(">>", ">")
                ;
            if (!s.Contains("!\\"))
                s = s.Replace("!", "<fullstop>");
            string rawS = s;
            foreach (string validTag in ValidTagSet)
                rawS = rawS.Replace(validTag, "");
            rawS = rawS
                .Replace("-\\BINDESTRICH", "")
                .Replace("!\\AUSRUFEZEICHEN", "");
            foreach(char c in rawS)
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

        private (string o, string c) ReorgPairStrings(string o, string c, int index, string filePath, string batchName, string taskName, string audioName, string taskId, string audioId)
        {
            try
            {
                var r = ValidateTimeStamp(o, c);
                r = ValidateTags(r.o, r.c);
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
        #endregion

        #region Offline data to input

        #endregion
    }

    class TextGridParser
    {
        public TextGridParser() { }

        Regex NameReg = new Regex("^\\s*name\\s*=\\s*\"(.*)\"\\s*$", RegexOptions.Compiled);
        Regex IntervalStartReg = new Regex("^\\s*intervals\\s*\\[([0-9]+)\\]:\\s*$", RegexOptions.Compiled);

        private IEnumerable<Interval> ExtractToIntervals(IEnumerable<string> textgridSeq)
        {
            string currentName = null;
            foreach(string s in textgridSeq)
            {
                if (NameReg.IsMatch(s))
                {
                    currentName = NameReg.Match(s).Groups[1].Value;
                }
            }
            throw new NotImplementedException();
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
