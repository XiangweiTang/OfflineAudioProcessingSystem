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
    }
}
