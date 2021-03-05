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
    static class TextGrid
    {
        public static bool Reject { get; private set; } = false;
        public static List<string> AllList = new List<string>();
        const double DIFF_THRESHOLD = 1;
        public static void Test()
        {
        }
        public static void TextGridToText(string textGridPath, string textPath, string sgIntermeidaPath, string hgIntermediaPath)
        {
            Reject = false;
            AllList = new List<string>();
            var textgridTextList = File.ReadLines(textGridPath);
            var textList = GetIntervalsNew(textgridTextList);
            OutputIntermediaIntervals(textList, sgIntermeidaPath, hgIntermediaPath);

            var outputList = MergeIntermediaIntervals(sgIntermeidaPath, hgIntermediaPath).ToArray();
            if (outputList.Length == 0)
            {
                AllList.Add($"Empty\t{textGridPath}");
            }
            if (!Reject)
                File.WriteAllLines(textPath, outputList);
        }
        static Regex SpaceReg = new Regex("\\s+", RegexOptions.Compiled);
        static Regex IntervalReg = new Regex("\\s*intervals\\s*\\[[0-9]+\\]:", RegexOptions.Compiled);

        private static void OutputIntermediaIntervals(IEnumerable<TextGridInterval> sequence, string sgPath, string hgPath)
        {
            List<TextGridInterval> hgList = new List<TextGridInterval>();
            List<TextGridInterval> sgList = new List<TextGridInterval>();
            foreach (var interval in sequence)
            {
                string content = GetContent(interval);
                if (!string.IsNullOrEmpty(content) && GetRaw(content) != "<unknown/>")
                {
                    if (interval.Item == SG_TAG)
                        sgList.Add(interval);
                    else
                        hgList.Add(interval);
                }
            }
            var hg = hgList.Select(x =>
            {
                x.Content = GetContent(x);
                return new TextGridLine(x).Output();
            });
            var sg = sgList.Select(x =>
            {
                x.Content = GetContent(x);
                return new TextGridLine(x).Output();
            });
            File.WriteAllLines(sgPath, sg);
            File.WriteAllLines(hgPath, hg);
        }

        private static IEnumerable<string> MergeIntermediaIntervals(string sgPath, string hgPath)
        {
            var sgList = File.ReadLines(sgPath).Select(x => new TextGridLine(x)).ToArray();
            var hgList = File.ReadLines(hgPath).Select(x => new TextGridLine(x)).ToArray();
            Sanity.Requires(hgList.Length == sgList.Length, "Interval count mismatch.");
            for(int i = 0; i < sgList.Length; i++)
            {
                double diff1 = Math.Abs(double.Parse(sgList[i].XMin) - double.Parse(hgList[i].XMin));
                double diff2 = Math.Abs(double.Parse(sgList[i].XMax) - double.Parse(hgList[i].XMax));
                Sanity.Requires(diff1 <= 1 && diff2 <= 1, "Time stamp mismatch.");
                yield return sgList[i].OutputSpecific();
                yield return hgList[i].OutputSpecific();
            }
        }
        private static IEnumerable<string> MergeIntervals(IEnumerable<TextGridInterval> sequence)
        {
            List<TextGridInterval> hgList = new List<TextGridInterval>();
            List<TextGridInterval> sgList = new List<TextGridInterval>();
            foreach(var interval in sequence)
            {
                string content = GetContent(interval);
                if (!string.IsNullOrEmpty(content)&&GetRaw(content)!="<unknown/>")
                {
                    if (interval.Item == SG_TAG)
                        sgList.Add(interval);
                    else
                        hgList.Add(interval);
                }
            }
            Sanity.Requires(hgList.Count == sgList.Count, "Interval count mismatch.");
            for(int i = 0; i < sgList.Count; i++)
            {
                double diff1 = Math.Abs(double.Parse(sgList[i].XMin) - double.Parse(hgList[i].XMin));
                double diff2 = Math.Abs(double.Parse(sgList[i].XMax) - double.Parse(hgList[i].XMax));
                Sanity.Requires(diff1 <= DIFF_THRESHOLD && diff2 <= DIFF_THRESHOLD, "Time stamp mismatch.");
                foreach (string s in OutputInterval(sgList[i], hgList[i]))
                    yield return s;
            }
        }
        private static string GetContent(TextGridInterval interval)
        {
            return string.Join(" ", interval.ContentList).Trim(' ', '"');
        }
        private static IEnumerable<string> OutputInterval(TextGridInterval interval1, TextGridInterval interval2)
        {
            yield return $"[{interval1.XMin} {interval1.XMax}] S1 <{interval1.Item}> {GetContent(interval1)} <{interval1.Item}/>";
            yield return $"[{interval1.XMin} {interval1.XMax}] S1 <{interval2.Item}> {GetContent(interval2)} <{interval2.Item}/>";
        }
        const string SG_TAG = "chdialects";
        const string HG_TAG = "chdialects-converted";
        private static IEnumerable<TextGridInterval> GetIntervalsNew(IEnumerable<string> textgridText)
        {
            TextGridInterval interval = new TextGridInterval();
            bool inSg = false;
            bool inHg = false;
            bool inInterval = false;
            foreach (string s in textgridText)
            {
                string sRaw = GetRaw(s);
                if (sRaw == "name=\"sg\"")
                {
                    inSg = true;
                    inHg = false;
                    continue;
                }
                if (sRaw == "name=\"hg\"")
                {
                    inHg = true;
                    inSg = false;
                    continue;
                }
                if (!inSg && !inHg)
                    continue;
                if (inSg && inHg)
                    throw new CommonException("Item wrong, cannot be both in SG and HG.");
                if (sRaw.StartsWith("item["))
                {
                    inInterval = false;
                    if (inSg)
                        interval.Item = SG_TAG;
                    else
                        interval.Item = HG_TAG;
                    yield return interval;
                    continue;
                }
                if (IntervalReg.IsMatch(s))
                {
                    if (inInterval)
                    {
                        if (inSg)
                            interval.Item = SG_TAG;
                        else
                            interval.Item = HG_TAG;
                        yield return interval;
                    }
                    interval = new TextGridInterval();
                    inInterval = true;
                    continue;
                }
                if (inInterval)
                {
                    if (sRaw.StartsWith("xmin="))
                    {
                        interval.XMin = sRaw.Split('=')[1];
                        continue;
                    }
                    if (sRaw.StartsWith("xmax="))
                    {
                        interval.XMax = sRaw.Split('=')[1];
                        continue;
                    }
                    if (sRaw.StartsWith("text="))
                    {
                        Sanity.Requires(interval.ContentList==null, "Content is not empty");
                        interval.ContentList = new List<string>();
                        interval.ContentList.Add(s.Split('=')[1]);
                        continue;
                    }
                    else
                    {
                        interval.ContentList.Add(s);
                    }
                }
            }
            if (inSg)
                interval.Item = SG_TAG;
            else
                interval.Item = HG_TAG;
            yield return interval;
        }
        
        private static string GetRaw(string s)
        {
            return SpaceReg.Replace(s, "").Trim().ToLower();
        }
        public static void TimeStampToTextGrid(string timeStampPath, string textGridPath)
        {
            var array = File.ReadAllLines(timeStampPath);
            List<string> list = new List<string>();
            list.Add(SetTextGridHeader(array));
            list.Add(SetTextGridItemHeader(array, 1));
            list.AddRange(SetTextGridBody(array));
            list.Add(SetTextGridItemHeader(array, 2));
            list.AddRange(SetTextGridBody(array));
            File.WriteAllLines(textGridPath, list);
        }

        private static string SetTextGridHeader(string[] timeStampArray)
        {
            string internalPath = "OfflineAudioProcessingSystem.Internal.Data.TextGridHeader.txt";
            string s = IO.ReadEmbedAll(internalPath, "OfflineAudioProcessingSystem");
            string xmin = timeStampArray[0].Split('\t')[0];
            string xmax = timeStampArray.Last().Split('\t')[1];
            return string.Format(s, xmin, xmax);
        }

        private static string SetTextGridItemHeader(string[] timeStampArray, int itemID)
        {
            string internalPath = "OfflineAudioProcessingSystem.Internal.Data.TextGridItemHeader.txt";
            string s = IO.ReadEmbedAll(internalPath, "OfflineAudioProcessingSystem");
            string xmin = timeStampArray[0].Split('\t')[0];
            int n = timeStampArray.Length;
            string xmax = timeStampArray[n - 1].Split('\t')[1];
            return string.Format(s, itemID, xmin, xmax, n);            
        }

        private static IEnumerable<string> SetTextGridBody(string[] timeStampArray)
        {
            string internalPath = "OfflineAudioProcessingSystem.Internal.Data.TextGridBody.txt";
            string s = IO.ReadEmbedAll(internalPath, "OfflineAudioProcessingSystem");
            for (int i = 0; i < timeStampArray.Length; i++)
            {
                string xmin = timeStampArray[i].Split('\t')[0];
                string xmax = timeStampArray[i].Split('\t')[1];
                yield return string.Format(s, i + 1, xmin, xmax);
            }
        }
    }

    struct TextGridInterval
    {
        public List<string> ContentList { get; set; }
        public string XMin { get; set; }
        public string XMax { get; set; }
        public string Item { get; set; }
        public string Content { get; set; }
    }

    class TextGridLine : Line
    {
        public string XMin { get; set; }
        public string XMax { get; set; }
        public string Item { get; set; }
        public string Content { get; set; }
        public TextGridLine(string s) : base(s) { }
        public TextGridLine(TextGridInterval interval)
        {
            XMin = interval.XMin;
            XMax = interval.XMax;
            Item = interval.Item;
            Content = interval.Content;
        }
        protected override IEnumerable<object> GetLine()
        {
            yield return XMin;
            yield return XMax;
            yield return Item;
            yield return Content;
        }

        protected override void SetLine(string[] split)
        {
            XMin = split[0];
            XMax = split[1];
            Item = split[2];
            Content = split[3];
        }

        public string OutputSpecific()
        {
            return $"[{XMin} {XMax}] S1 <{Item}> {Content} <{Item}/>";
        }
    }
}
