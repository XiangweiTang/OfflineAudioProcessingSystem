﻿using System;
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
    }
}
