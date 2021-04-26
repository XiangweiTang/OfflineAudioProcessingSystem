using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Common;

namespace OfflineAudioProcessingSystem.TranscriptValidation
{
    class ValidateAndUpdateFolder
    {
        (string WrongTag, string CorrectTag)[] ReplaceArray = new (string WrongTag, string NewTag)[0];
        HashSet<string> ValidTagSet = new HashSet<string>();
        List<string> ErrorList = new List<string>();
        List<string> ReportList = new List<string>();
        public ValidateAndUpdateFolder()
        {
            Init();
        }

        private void Init()
        {
            ReplaceArray = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.HighFreqErrorTagReplacement.txt", "OfflineAudioProcessingSystem")
                .Select(x => (x.Split('\t')[0], x.Split('\t')[1]))
                .ToArray();
            ValidTagSet = IO.ReadEmbed("OfflineAudioProcessingSystem.Internal.Data.ValidTag.txt", "OfflineAudioProcessingSystem").ToHashSet();
        }
        
        public void Validate(string folderPath)
        {
            if(folderPath.ToLower().EndsWith("online"))
            {
                if (!new OneOffTotalMapping().SetByInputOnline(folderPath))
                    return;
                string batchName = folderPath.Split('\\').Last();
                var list = File.ReadLines(Path.Combine(folderPath, "FromAnnotation.txt"))
                    .Select(x => new AnnotationLine(x));
                foreach (var line in list)
                    UpdateSingleFile(folderPath, line, batchName);
                File.WriteAllLines(Path.Combine(folderPath, "AllFilter.txt"), ErrorList);
                File.WriteAllLines(Path.Combine(folderPath, "ToAnnotator.txt"), ReportList);
            }
        }

        public void UpdateSingleFile(string inputFolderPath, AnnotationLine line, string batchName)
        {            
            string audioFilePath = Path.Combine(inputFolderPath, "Input", $"{line.TaskId}_{line.TaskName}", "Speaker", $"{line.AudioName}");
            string textFilePath = audioFilePath.Substring(0, audioFilePath.Length - 3) + "txt";
            if (textFilePath == @"F:\WorkFolder\Transcripts\20210422_Online\Input\750_20210201_Chur_10014\Speaker\00005.txt")
                ;
            if (!File.Exists(textFilePath))
                textFilePath = audioFilePath.Replace(".wav", ".txt");
            Sanity.Requires(File.Exists(textFilePath));
            ValidateAndUpdateFile v = new ValidateAndUpdateFile(ReplaceArray, ValidTagSet);
            v.ValidateAndCorrectFile(textFilePath, batchName, line.TaskName, line.AudioName, line.TaskId.ToString(), line.AudioPlatformId.ToString());
            ErrorList.AddRange(v.ErrorList);
            ReportList.AddRange(v.ToAnnotatorList);
        }
    }    
}
