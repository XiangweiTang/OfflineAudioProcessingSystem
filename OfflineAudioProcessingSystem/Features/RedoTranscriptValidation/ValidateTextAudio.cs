using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;
using System.Text.RegularExpressions;

namespace OfflineAudioProcessingSystem.RedoTranscriptValidation
{
    class ValidateTextAudio
    {        
        Regex SpaceReg = new Regex("\\s+", RegexOptions.Compiled);
        char[] Sep = { ' ' };
        string[] Errors =
        {
            "Succeed",  //0
            "File line count is less than 4.",  //1
            "SG HG not at start.",  //2

            "Incomplete tag",   //3
            "Digit", //4
            "Invalid char",    //5

            "SG too long",  //6
            "HG too long",  //7
            "Tag mismatch", //8

            "LineCountIsNotEven",   //9
            "LineCountTooShort",    //10

            "Not3TSV",      //11
            "SG HG index mismatch",  //12
            "SG HG label error",    //13

            "String is empty",      //

            "Miss source file.",    //15
            "SentenceCount mismatch",   //16
        };
        HashSet<string> ValidTagSet = new HashSet<string>();
        public ValidateTextAudio()
        {
            Init();
        }
        private void Init()
        {
            ValidTagSet = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.ValidTag.txt", "OfflineAudioProcessingSystem")
                .ToHashSet();
        }
        private (string sg, string hg) ExtractSgHg(string filePath)
        {
            var array = File.ReadLines(filePath)
                .Select(x => x.Trim())
                .ToArray();
            Sanity.Requires(array.Length >= 4, filePath, 1);
            int sgIndex = Array.IndexOf(array, "SG");
            int hgIndex = Array.IndexOf(array, "HG");
            Sanity.Requires(sgIndex == 0 || hgIndex == 0, filePath, 2);
            if (sgIndex == 0)
            {
                return ExtractContent(array, hgIndex);                
            }
            else
            {
                var r = ExtractContent(array, sgIndex);
                return (r.Item2, r.Item1);
            }
        }           
        private (string,string) ExtractContent(string[] r, int index)
        {
            StringBuilder sb1 = new StringBuilder();
            for (int i = 1; i < index; i++)
            {
                sb1.Append(" ");
                sb1.Append(r[i]);
            }

            StringBuilder sb2 = new StringBuilder();
            for(int i = index + 1; i < r.Length; i++)
            {
                sb2.Append(" ");
                sb2.Append(r[i]);
            }

            return (sb1.ToString().Trim(), sb2.ToString().Trim());
        }
        List<string> SubErrorList = new List<string>();
        public List<string> TotalErrorList = new List<string>();
        public void ValidateAndUpdateFile(string inputFilePath, string outputFilePath, bool output)
        {
            try
            {
                var sghg = ExtractSgHg(inputFilePath);
                string sg = sghg.sg;
                string hg = sghg.hg;

                string sgClean = PostCleanupStrings(ValidateAndUpdateContent(sg));
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tSG\t{x}"));
                string hgClean = PostCleanupStrings(ValidateAndUpdateContent(hg));
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tHG\t{x}"));
                
                PostCheckPairs(sgClean, hgClean);
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tPairMismatch\t{x}"));

                if (output)
                {
                    string[] o = { sgClean, hgClean };
                    File.WriteAllLines(outputFilePath, o);
                }
            }
            catch(CommonException ce)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\t{Errors[ce.HResult]}\t{ce.Message}");
            }
            catch(Exception e)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\tOtherErrors\t{e.Message}");
            }
        }

        public void ValidateAndUpdateFileTotal(string inputFilePath, string outputFilePath, bool output)
        {
            try
            {
                bool valid = true;
                string subPath = inputFilePath.ToLower().Replace(@"postdelivery\input\20210622", "300hrsannotation");
                Sanity.Requires(File.Exists(subPath), "TotalMismatch", 15);
                int srcLength = File.ReadAllLines(subPath).Length;
                int tgtLength = File.ReadAllLines(inputFilePath).Length;
                Sanity.Requires(srcLength == tgtLength, "TotalMismatch", 16);
                string[] array = File.ReadAllLines(inputFilePath);
                Sanity.Requires(array.Length % 2 == 0, array.Length.ToString(), 9);
                Sanity.Requires(array.Length >= 2, array.Length.ToString(), 10);
                List<string> outputList = new List<string>();
                for(int i = 0; i < array.Length; i += 2)
                {
                    string sg = array[i];
                    string hg = array[i + 1];
                    var block = CheckSgHg(inputFilePath, i / 2, sg, hg);
                    if (block == null)
                        valid = false;
                    else
                        outputList.AddRange(block);
                }
                if (output & valid)
                    File.WriteAllLines(outputFilePath, outputList);
            }
            catch (CommonException ce)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\t{Errors[ce.HResult]}\t{ce.Message}");
            }
            catch (Exception e)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\tOtherErrors\t{e.Message}");
            }
        }

        public void ValidateAndUpdateFileTotalTransLine(string inputFilePath, string outputFilePath, bool output)
        {
            try
            {
                string[] array = File.ReadAllLines(inputFilePath);
                Sanity.Requires(array.Length % 2 == 0, array.Length.ToString(), 9);
                Sanity.Requires(array.Length >= 2, array.Length.ToString(), 10);
                List<string> total = new List<string>();
                for (int i = 0; i < array.Length; i += 2)
                {
                    string sg = LocalCommon.ExtractTransLine(array[i]).Content;
                    string hg = LocalCommon.ExtractTransLine(array[i + 1]).Content;
                    var r=CheckSgHgTransLine(inputFilePath, sg, hg);
                    var l1 = LocalCommon.ExtractTransLine(array[i]);
                    l1.Content = r[0];
                    var l2 = LocalCommon.ExtractTransLine(array[i + 1]);
                    l2.Content = r[1];
                    total.Add(l1.OutputTransLine());
                    total.Add(l2.OutputTransLine());
                }
                File.WriteAllLines(outputFilePath, total);
            }
            catch (CommonException ce)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\t{Errors[ce.HResult]}\t{ce.Message}");
            }
            catch (Exception e)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\tOtherErrors\t{e.Message}");
            }
        }

        private List<string> CheckSgHgTransLine(string inputFilePath, string sg, string hg)
        {
            try
            {
                string sgClean = PostCleanupStrings(ValidateAndUpdateContent(sg));
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tSG\t{x}"));
                string hgClean = PostCleanupStrings(ValidateAndUpdateContent(hg));
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tHG\t{x}"));

                PostCheckPairs(sgClean, hgClean);
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tPairMismatch\t{x}"));

                return new List<string> { sgClean, hgClean };
            }
            catch (CommonException ce)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\t{Errors[ce.HResult]}\t{ce.Message}");
            }
            catch (Exception e)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\tOtherErrors\t{e.Message}");
            }
            return null;
        }
        private List<string> CheckSgHg(string inputFilePath, int index, string sgWhole, string hgWhole)
        {            
            try
            {
                List<string> list = new List<string>();
                var sgChunk = GetContent(sgWhole, "SG");
                var hgChunk = GetContent(hgWhole, "HG");
                Sanity.Requires(sgChunk.index == hgChunk.index, sgWhole, 13);
                Sanity.Requires(sgChunk.index == index, 13);
                string sg = sgChunk.content;
                string hg = hgChunk.content;
                string s1 = ValidateAndUpdateContent(sg);
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tSG\t{x}"));
                if (s1 == null)
                    return null;
                string sgClean = PostCleanupStrings(s1);
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tSG\t{x}"));
                string s2 = ValidateAndUpdateContent(hg);
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tHG\t{x}"));
                if (s2 == null)
                    return null;
                string hgClean = PostCleanupStrings(s2);
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tHG\t{x}"));

                //PostCheckPairs(sgClean, hgClean);
                TotalErrorList.AddRange(SubErrorList.Select(x => $"{inputFilePath}\tPairMismatch\t{x}"));

                list.Add(sgClean);
                list.Add(hgClean);
                return list;
            }
            catch (CommonException ce)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\t{Errors[ce.HResult]}\t{ce.Message}");
            }
            catch (Exception e)
            {
                TotalErrorList.Add($"{inputFilePath}\tFormat\tOtherErrors\t{e.Message}");
            }
            return null;
        }

        private (int index,string content) GetContent(string whole, string label)
        {
            var split = whole.Split('\t');
            Sanity.Requires(split.Length == 3, whole, 11);
            Sanity.Requires(split[1] == label, whole, 12);
            int index;
            Sanity.Requires(int.TryParse(split[0], out index), whole, 13);
            return (index, split[2]);
        }

        private void PostCheckPairs(string sg, string hg)
        {
            SubErrorList = new List<string>();
            try
            {
                var sgSplit = sg.Split(' ');
                var hgSplit = hg.Split(' ');
                int sgLength = sgSplit.Length;
                int hgLength = hgSplit.Length;
                Sanity.Requires(ValidLength(sgLength, hgLength), 6);
                Sanity.Requires(ValidLength(hgLength, sgLength), 7);
                var sgTags = sgSplit.Where(x => x[0] == '<');
                var hgTags = hgSplit.Where(x => x[0] == '<');
                Sanity.Requires(sgTags.SequenceEqual(hgTags), 8);
            }
            catch(CommonException ce)
            {
                SubErrorList.Add($"{Errors[ce.HResult]}\t{sg}");
                SubErrorList.Add($"{Errors[ce.HResult]}\t{hg}");
            }
        }

        private bool ValidLength(int longer, int shorter)
        {
            if (longer <= shorter * 1.5)
                return true;
            if (shorter <= 3 && longer <= shorter * 3)
                return true;
            return false;
        }
        private string PostCleanupStrings(string s)
        {
            s = s.Replace(">", "> ")
                .Replace("<", " <");
            return SpaceReg.Replace(s, " ").Trim();
        }
        private string ValidateAndUpdateContent(string s)
        {
            SubErrorList = new List<string>();
            List<string> outputList = new List<string>();
            s = s.Replace("<", " <").Replace(">", "> ").Replace("<UNKNOWN>", "<UNKNOWN/>");
            var split = s.Split(Sep, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            foreach(string token in split)
            {
                if (token == "")
                    continue;
                try
                {
                    outputList.Add(ValidateToken(token));
                }
                catch(CommonException ce)
                {                    
                    SubErrorList.Add($"{Errors[ce.HResult]}\t{ce.Message}");
                    outputList.Add(token);
                    return null;
                }
                catch(Exception e)
                {
                    SubErrorList.Add($"OtherErrors\t{e.Message}");
                    outputList.Add(token);
                    return null;
                }
            }
            return string.Join(" ", outputList);
        }
        private string ValidateToken(string token)
        {
            if (token[0] == '<' || token.Last() == '>')
            {
                return ValidateTag(token);
            }
            StringBuilder sb = new StringBuilder();
            foreach(char c in token)
            {
                if (c >= 'a' && c <= 'z')
                {
                    sb.Append(c);
                    continue;
                }
                if (c >= 'A' && c <= 'Z')
                {
                    sb.Append(c);
                    continue;
                }
                if (c >= '\u00c0' && c <= '\u017f')
                {
                    sb.Append(c);
                    continue;
                }
                if (c == '-' || c == '\'' || c == 946)
                {
                    sb.Append(c);
                    continue;
                }
                if(c==':'||c==','||c==';'||c== '，')
                {
                    sb.Append(" <comma> ");
                    continue;
                }
                if (c == '。' || c == '.')
                {
                    sb.Append(" <fullstop> ");
                    continue;
                }
                if (c == '?')
                {
                    sb.Append(" <questionmark> ");
                    continue;
                }
                if (c == '´'||c== '`'||c== '’')
                {
                    sb.Append('\'');
                    continue;
                }
                if (c == '(' || c == ')' || c == '"'||c=='\u00bb'||c=='\u00ad')
                {
                    sb.Append(' ');
                    continue;
                }
                if (c == '<' || c == '>')
                    Sanity.Throw(token, 3);
                if (c >= '0' && c <= '9')
                    Sanity.Throw(token, 4);
                Sanity.Throw(token, 5);
            }
            return sb.ToString();
        }
        private string ValidateTag(string token)
        {
            if (ValidTagSet.Contains(token))
                return token;
            if (ValidTagSet.Contains(token.ToUpper()))
                return token.ToUpper();
            if (ValidTagSet.Contains(token.ToLower()))
                return token.ToLower();
            
            throw new CommonException(token, 3);
        }
    }
    class ValidateTransferTransLine : FolderTransfer
    {
        ValidateTextAudio Vta = new ValidateTextAudio();
        string ReportPath = "";
        public ValidateTransferTransLine(string reportPath)
        {
            ReportPath = reportPath;
        }
        protected override IEnumerable<string> GetItems(string inputFolderPath)
        {
            return Directory.EnumerateFiles(inputFolderPath, "*.txt");
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            Vta.ValidateAndUpdateFileTotalTransLine(inputPath, outputPath, false);
        }

        protected override void PostRun()
        {
            File.WriteAllLines(ReportPath, Vta.TotalErrorList);
        }
    }
    class ValidateTransfer : FolderTransfer
    {
        ValidateTextAudio Vta = new ValidateTextAudio();
        string ReportPath = "";
        public ValidateTransfer(string reportPath)
        {
            ReportPath = reportPath;
        }
        protected override IEnumerable<string> GetItems(string inputFolderPath)
        {
            return Directory.EnumerateFiles(inputFolderPath, "*.txt");
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            Vta.ValidateAndUpdateFileTotal(inputPath, outputPath, true);
        }

        protected override void PostRun()
        {
            File.WriteAllLines(ReportPath, Vta.TotalErrorList);
        }
    }

    static class RunValidateTransfer
    {
        public static void RunAll(string timeStamp)
        {
            RunValidation(timeStamp);
            CheckSoundHour(timeStamp);
            //RandomSample(timeStamp);
        }

        public static void RunValidationTransLine(string timeStamp)
        {
            string i = Path.Combine(@"F:\WorkFolder\PostDelivery\Transline", timeStamp);
            string o = Path.Combine(@"F:\WorkFolder\PostDelivery\TranslineOutput", timeStamp);
            string r = Path.Combine(@"F:\WorkFolder\PostDelivery\Report", timeStamp + ".Report.txt");
            ValidateTransferTransLine vtt = new ValidateTransferTransLine(r);
            vtt.Run(i, o);
        }

        public static void TransferTextGrid(string timeStamp)
        {
            string g = Path.Combine(@"F:\WorkFolder\PostDelivery\TextGrid", timeStamp);
            string t = Path.Combine(@"F:\WorkFolder\PostDelivery\Transline", timeStamp);
            TextGridTransfer tt = new TextGridTransfer(timeStamp);
            tt.Run(g, t);
        }
        public static void RunValidation(string timeStamp)
        {
            string i = Path.Combine(@"F:\WorkFolder\PostDelivery\Input", timeStamp);
            string o = Path.Combine(@"F:\WorkFolder\PostDelivery\Output", timeStamp);
            string r = Path.Combine(@"F:\WorkFolder\PostDelivery\Report", timeStamp + ".Report.txt");
            ValidateTransfer vt = new ValidateTransfer(r);
            vt.Run(i, o, 1);
        }

        public static void CheckSoundHour(string timeStamp)
        {
            string rootPath = Path.Combine(@"F:\WorkFolder\PostDelivery\Input", timeStamp);
            string reportPath= Path.Combine(@"F:\WorkFolder\PostDelivery\Report", timeStamp + ".SoundHour.txt");
            List<string> outputList = new List<string>();
            double total = 0;
            foreach (string dialectPath in Directory.EnumerateDirectories(rootPath))
            {
                string dialect = dialectPath.Split('\\').Last();                
                double seconds = 0;
                foreach(string speakerPath in Directory.EnumerateDirectories(dialectPath))
                {
                    string speaker = speakerPath.Split('\\').Last();
                    foreach(string textPath in Directory.EnumerateFiles(speakerPath))
                    {
                        string fileId = textPath.Split('\\').Last().Split('.')[0];

                        string audioPath = Path.Combine(@"F:\WorkFolder\300hrsRecording", dialect, speaker, fileId + ".wav");
                        long l = new FileInfo(audioPath).Length;
                        double d = (double)l / 32000;
                        seconds += d;
                    }
                }
                total += seconds;
                outputList.Add($"{dialect}\t{seconds:0.00}\t{seconds / 3600:0.00}");
            }
            outputList.Add($"Total\t{total:0.00}\t{total / 3600:0.00}");
            File.WriteAllLines(reportPath, outputList);
        }

        public static void RandomSample(string timeStamp)
        {
            var seq = BreakDown(timeStamp);
            var r = seq.Shuffle();
            double d = 0;
            int subId = 0;
            string reportPath = Path.Combine(@"F:\WorkFolder\PostDelivery\RandomSample", timeStamp, "Total.txt");
            List<string> reportList = new List<string>();
            for(int i = 0; i < r.Length; i++)
            {
                if (d >= 1800)
                {
                    subId = 1;
                }
                if (d >= 3600)
                {
                    break;
                }
                var item = r[i];
                string folderPath = Path.Combine(@"F:\WorkFolder\PostDelivery\RandomSample", timeStamp, subId.ToString(), item.dialect);
                Directory.CreateDirectory(folderPath);
                string audioPath = Path.Combine(folderPath, i.ToString("00000") + ".wav");
                string textPath = Path.Combine(folderPath, i.ToString("00000") + ".txt");
                d += item.audioHour;
                File.Copy(item.audioPath, audioPath);
                File.WriteAllLines(textPath, item.content.ToSequence());
                reportList.Add($"{subId}\t{item.dialect}\t{audioPath}\t{item.audioPath}\t{item.content}");
            }
            File.WriteAllLines(reportPath, reportList.OrderBy(x => x));
        }

        private static IEnumerable<(string audioPath, string dialect, string content,double audioHour)> BreakDown(string timeStamp)
        {
            string rootPath = Path.Combine(@"F:\WorkFolder\PostDelivery\Input", timeStamp);            
            foreach (string dialectPath in Directory.EnumerateDirectories(rootPath))
            {
                string dialect = dialectPath.Split('\\').Last();
                foreach (string speakerPath in Directory.EnumerateDirectories(dialectPath))
                {
                    string speaker = speakerPath.Split('\\').Last();
                    foreach (string textPath in Directory.EnumerateFiles(speakerPath))
                    {
                        string fileId = textPath.Split('\\').Last().Split('.')[0];
                        List<(string, string)> list = new List<(string, string)>();
                        try { list = GetSgHg(textPath); }
                        catch { }
                        foreach(var item in list)
                        {
                            string audioPath = Path.Combine(@"F:\WorkFolder\300hrsSplit", dialect, speaker, fileId, item.Item1 + ".wav");
                            string content = item.Item2;
                            double audioHour = ((double)new FileInfo(audioPath).Length) / 32000;
                            yield return (audioPath, dialect, content, audioHour);
                        }
                    }
                }
            }
        }

        private static List<(string,string)> GetSgHg(string filepath)
        {
            var array = File.ReadAllLines(filepath);
            List<(string, string)> list = new List<(string, string)>();
            for(int i = 0; i < array.Length; i += 2)
            {
                string sg = array[i].Split('\t')[2];
                string hg = array[i + 1].Split('\t')[2];
                string id = array[i].Split('\t')[0];
                list.Add((id, $"{sg}\t{hg}"));
            }
            return list;
        }
    }

    class TextGridTransfer : FolderTransfer
    {
        List<string> ErrorList = new List<string>();
        string ReportPath;
        public TextGridTransfer(string timeStamp)
        {
            ReportPath = Path.Combine(@"F:\WorkFolder\PostDelivery\TextGrid", timeStamp + ".txt");
        }
        public override string ItemRename(string inputItemName)
        {
            return inputItemName.ToLower().Replace(".textgrid", ".txt");
        }
        protected override void ItemTransfer(string inputPath, string outputPath)
        {
            TextGrid.TextGridToText(inputPath, outputPath, inputPath + ".sg", inputPath + ".hg", false);
            ErrorList.AddRange(TextGrid.AllList);
            TextGrid.AllList.Clear();
        }

        protected override void PostRun()
        {
            File.WriteAllLines(ReportPath, ErrorList);
        }
    }
}
