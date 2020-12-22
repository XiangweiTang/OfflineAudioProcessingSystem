using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;

namespace OfflineAudioProcessingSystem
{
    public static class LocalCommon
    {
        public static string SoxPath { get; set; }
        public static string FfmpegPath { get; set; }
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
            string arguments = $"{inputAudioPath} {outputAudioPath} trim {startTime} {duration}";
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
        public static bool AudioIdentical(string waveFilePath1, string waveFilePath2)
        {
            const int BUFFER_SIZE = 10_000;
            Wave w1 = new Wave();
            w1.ShallowParse(waveFilePath1);
            Wave w2 = new Wave();
            w2.ShallowParse(waveFilePath1);
            using (FileStream fs1 = new FileStream(waveFilePath1, FileMode.Open, FileAccess.Read))
            using (FileStream fs2 = new FileStream(waveFilePath2, FileMode.Open, FileAccess.Read))
            {
                fs1.Seek(w1.DataChunk.Offset + 8, SeekOrigin.Begin);
                fs2.Seek(w2.DataChunk.Offset + 8, SeekOrigin.Begin);
                byte[] buffer1 = new byte[BUFFER_SIZE];
                byte[] buffer2 = new byte[BUFFER_SIZE];
                while (fs1.Position < fs1.Length)
                {
                    int count = Math.Min(BUFFER_SIZE, (int)(fs1.Length - fs1.Position));
                    try
                    {
                        fs1.Read(buffer1, 0, BUFFER_SIZE);
                        fs2.Read(buffer2, 0, BUFFER_SIZE);
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
        }
    }
}
