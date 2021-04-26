using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;
using System.Text.RegularExpressions;

namespace OfflineAudioProcessingSystem
{
    public static class LocalCommon
    {
        public static string SoxPath { get; set; }
        public static string FfmpegPath { get; set; }
        public static string VadScriptPath { get; set; }
        public static string PythonPath { get; set; }
        private static HashSet<(string, string)> LocaleRegStringSet = new HashSet<(string, string)>();
        public static Dictionary<string, PathMappingLine> DeliveredWavDict = new Dictionary<string, PathMappingLine>();
        public static Dictionary<string, PathMappingLine> DeliveredTxtDict = new Dictionary<string, PathMappingLine>();
        static LocalCommon()
        {
            LocaleRegStringSet = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.LocaleRegexMapping.txt", "OfflineAudioProcessingSystem")
                .Select(x => (x.Split('\t')[0], x.Split('\t')[1])).ToHashSet();
            SetDict();
        }
        private static void SetDict()
        {
            foreach(string s in File.ReadLines(@"F:\WorkFolder\Summary\20210222\Important\TransMapping.txt"))
            {
                var line = new PathMappingLine(s);
                DeliveredTxtDict.Add(line.NewTextPath.ToLower(), line);
                DeliveredWavDict.Add(line.NewWavePath.ToLower(), line);
            }
        }
        public static void RunFfmpeg(string arguments)
        {
            RunFile.Run(FfmpegPath, arguments, false, "");
        }
        public static void RunSox(string arguments)
        {
            RunFile.Run(SoxPath, arguments, false, "");
        }
        public static void CutAudioWithSox(string inputAudioPath, object startTime, object duration, string outputAudioPath)
        {
            string arguments = $"{ inputAudioPath} {outputAudioPath} trim {startTime} {duration}";
            RunSox(arguments);
        }
        public static void MergeAudioWithFfmpeg(IEnumerable<string> inputAudioList, string outputAudioPath, string listPath)
        {
            var list = inputAudioList.Select(x => $"file '{x}'");
            File.WriteAllLines(listPath, list);

            string arguments = $"-f concat -i {listPath} -c copy {outputAudioPath}";
            RunFfmpeg(arguments);
        }
        public static void SetAudioToWaveWithFfmpeg(string inputAudioPath, string outputAudioPath)
        {
            string arguments = $"-i {inputAudioPath} {outputAudioPath}";
            RunFfmpeg(arguments);
        }
        public static void SetAudioWithFfmpeg(string inputAudioPath, int sampleRate, int channelNumber , string outputAudioPath)
        {
            string arguments = $"-i {inputAudioPath} -ar {sampleRate} -ac {channelNumber} {outputAudioPath}";
            RunFfmpeg(arguments);
        }

        public static void SetAudioWithSox(string inputAudioPath, int sampleRate, int channelNumber, string outputAudioPath)
        {
            string arguments = $"{inputAudioPath} -r {sampleRate} -c {channelNumber} {outputAudioPath}";
            RunSox(arguments);
        }

        public static void SetTimeStampsWithVad(string inputAudioPath, string outputTimeStampFilePath, int cutLevel)
        {
            // The python was from: https://github.com/wiseman/py-webrtcvad
            RunFile.RunPython(PythonPath, VadScriptPath, cutLevel.ToString(), inputAudioPath.WrapPath(), outputTimeStampFilePath.WrapPath());
        }

        public static bool BinaryIdentical(Stream fs1, int offset1, Stream fs2, int offset2)
        {
            const int BUFFER_SIZE = 10_000;
            fs1.Seek(offset1, SeekOrigin.Begin);
            fs2.Seek(offset2, SeekOrigin.Begin);
            byte[] buffer1 = new byte[BUFFER_SIZE];
            byte[] buffer2 = new byte[BUFFER_SIZE];
            while (fs1.Position < fs1.Length)
            {                
                try
                {
                    int p=fs1.Read(buffer1, 0, BUFFER_SIZE);
                    int q=fs2.Read(buffer2, 0, BUFFER_SIZE);
                    //Console.WriteLine($"{fs2.Position}\t{fs1.Position}");
                    //if (fs2.Position - fs1.Position == offset2 - offset1)
                    //    ;
                    //else
                    //    ;
                    if (buffer1.SequenceEqual(buffer2))
                       continue;
                    
                    return false;
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
        public static bool AudioidenticalAzure(string azureUri1, string azureUri2)
        {
            using (Stream st1 = AzureUtils.ReadBlobToStream(azureUri1))
            using (Stream st2 = AzureUtils.ReadBlobToStream(azureUri2))
            {
                Wave w1 = new Wave();
                w1.ShallowParse(st1);
                int offset1 = w1.DataChunk.Offset + 8;
                Wave w2 = new Wave();
                w2.ShallowParse(st2);
                int offset2 = w2.DataChunk.Offset + 8;
                return BinaryIdentical(st1, offset1, st2, offset2);
            }
        }
        public static bool AudioIdenticalLocal(string waveFilePath1, string waveFilePath2)
        {
            Wave w1 = new Wave();
            w1.ShallowParse(waveFilePath1);
            int offset1 = w1.DataChunk.Offset + 8;
            Wave w2 = new Wave();
            w2.ShallowParse(waveFilePath2);
            int offset2 = w2.DataChunk.Offset + 8;
            using (Stream fs1 = File.OpenRead(waveFilePath1))
            using (Stream fs2 = File.OpenRead(waveFilePath2))
            {
                return BinaryIdentical(fs1, offset1, fs2, offset2);
            }
        }
        public static IEnumerable<string> AudioIdenticalLocal(IEnumerable<string> newWaveList, IEnumerable<string> oldWaveList)
        {
            Dictionary<long, HashSet<string>> dupeDict = new Dictionary<long, HashSet<string>>();
            foreach(string oldWavePath in oldWaveList)
            {
                Wave w = new Wave();
                w.ShallowParse(oldWavePath);
                long key = w.DataChunk.Length;
                if (!dupeDict.ContainsKey(key))
                    dupeDict[key] = new HashSet<string> { oldWavePath };
                else
                    dupeDict[key].Add(oldWavePath);
            }


            foreach(string newWavePath in newWaveList)
            {
                Wave w = new Wave();
                w.ShallowParse(newWavePath);
                long key = w.DataChunk.Length;
                if (!dupeDict.ContainsKey(key))
                    dupeDict[key] = new HashSet<string> { newWavePath };
                else
                {                    
                    foreach(string oldWavPath in dupeDict[key])
                    {
                        if (AudioIdenticalLocal(newWavePath, oldWavPath))
                        {                            
                            yield return $"{newWavePath}\t{oldWavPath}";
                            break;
                        }                        
                    }

                    dupeDict[key].Add(newWavePath);
                }
            }
        }
        public static string GetLocale(string s)
        {
            foreach(var pair in LocaleRegStringSet)
            {
                Regex reg = new Regex(pair.Item1);
                if (reg.IsMatch(s.ToLower()))
                    return pair.Item2;
            }
            throw new CommonException(s, -1);
        }

        public static string BasicCleanup(string s)
        {
            return s.Replace(",", "<comma>")
                .Replace(".", "<fullstop>")
                .Replace("?", "<questionmark>");
        }

        public static string GetAnnotationKey(string content)
        {
            string timeStamp = content.Split(']')[0].Trim('[');
            string tag = Regex.Match(content, "<.*?>").Groups[0].Value;
            return $"[{timeStamp}|{tag}";
        }
        public static IEnumerable<(double,double)> GetTotalCoverage(IEnumerable<(double, double)> intervals, double threshold=1)
        {
            var r = intervals.ToArray();
            for (int i = 0; i < r.Length - 1; i++)
            {
                for (int j = i + 1; j < r.Length; j++)
                {
                    double diff= CompareIntervals(r[i], r[j]);
                    if(diff>threshold)
                    {
                        yield return r[i];
                        yield return r[j];
                    }
                }
            }
        }

        private static double CompareIntervals((double, double) interval1, (double, double) interval2)
        {
            if (!IntervalValid(interval1))
                return 0;
            if (!IntervalValid(interval2))
                return 0;
            if (interval1.Item2 < interval2.Item1)
                return 0;
            if (interval1.Item1 > interval2.Item2)
                return 0;
            double left = Math.Max(interval1.Item1, interval2.Item1);
            double right = Math.Min(interval1.Item2, interval2.Item2);
            return right - left;
        }

        private static bool IntervalValid((double, double) interval)
        {
            return interval.Item2 >= interval.Item1;
        }


        public static Regex TransLineRegex = new Regex("\\[([\\-0-9.]+)\\s+([0-9.]+)]\\s*([sS][12])\\s*(<.*?>)(.*)(<.*?>)", RegexOptions.Compiled);
        public static TransLine ExtractTransLine(string s)
        {
            Sanity.Requires(TransLineRegex.IsMatch(s));
            var match = TransLineRegex.Match(s);
            TransLine line = new TransLine
            {
                StartTimeString = match.Groups[1].Value,
                EndTimeString = match.Groups[2].Value,
                Speaker = match.Groups[3].Value.ToUpper(),
                Prefix = match.Groups[4].Value,
                Content = match.Groups[5].Value,
                Suffix = match.Groups[6].Value
            };
            line.StartTime = double.Parse(line.StartTimeString);
            line.EndTime = double.Parse(line.EndTimeString);
            return line;
        }

        public static string OutputTransLine(this TransLine line)
        {
            return $"[{line.StartTimeString} {line.EndTimeString}] {line.Speaker} {line.Prefix}{line.Content}{line.Suffix}";
        }
        public static string GetTransLineKey(TransLine line)
        {

            return $"{GetPrefix(line.StartTimeString, 8)}|{GetPrefix(line.EndTimeString, 8)}|{line.Prefix}";
        }
        public static string GetPrefix(string s, int n)
        {
            n = Math.Min(n, s.Length);
            return s.Substring(0, n);
        }
        public static void ModifyFileWithUpdate(string filePath, IEnumerable<TransLine> updates, bool updateFlag = false)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine(filePath);
                return;
            }
            var dict = File.ReadLines(filePath)
                .Select(x => ExtractTransLine(x))
                .ToDictionary(x => GetTransLineKey(x), x => x);
            int i = dict.Count;
            foreach(var update in updates)
            {
                string key = GetTransLineKey(update);
                Sanity.Requires(dict.ContainsKey(key));
                //else
                dict[GetTransLineKey(update)] = update;
            }
            Sanity.Requires(dict.Count == i);
            if(updateFlag)
            {
                var list = dict.Select(x => OutputTransLine(x.Value));
                File.WriteAllLines(filePath, list);
            }
        }
        public static string GetFilePathFromUpdate(string[] split)
        {
            string taskFolderName;
            if (split[0].ToLower().EndsWith("offline"))
                taskFolderName = split[1];
            else
                taskFolderName = $"{split[3]}_{split[1]}";

            string textName = split[2].Contains(".wav")
                ? split[2].Substring(0, split[2].Length - 4) + ".txt"
                : split[2] + ".txt";
            return Path.Combine(Constants.ANNOTATION_ROOT_PATH, split[0], "Input", taskFolderName, "Speaker", textName);
        }
        public static string GetFilePathFromUpdate(string s)
        {
            var split = s.Split('\t');
            return GetFilePathFromUpdate(split);
        }

        public static IEnumerable<string> GetOnlineFolders()
        {
            return Directory.EnumerateDirectories(@"F:\WorkFolder\Transcripts", "*online");
        }
        public static IEnumerable<string> GetOfflineFolder()
        {
            return Directory.EnumerateDirectories(@"F:\WorkFolder\Transcripts", "*offline");
        }
        public static readonly Regex SpaceReg = new Regex("\\s+", RegexOptions.Compiled);
        public static string CleanupSpace(this string s)
        {
            return SpaceReg.Replace(s, " ").Trim();
        }
        public static Dictionary<string,string> ReadToDict(string path)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (string s in File.ReadLines(path))
                dict.Add(s.Split('\t')[0], s.Split('\t')[1]);
            return dict;
        }
        public static Dictionary<string,HashSet<string>> ReadToMDict(string path)
        {
            return File.ReadLines(path)
                .GroupBy(x => x.Split('\t')[0])
                .ToDictionary(x => x.Key, x => x.Select(y => y.Split('\t')[1]).ToHashSet());
        }
    }
}
