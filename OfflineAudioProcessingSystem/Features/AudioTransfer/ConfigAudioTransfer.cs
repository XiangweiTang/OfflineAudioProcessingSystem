using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;

namespace OfflineAudioProcessingSystem.AudioTransfer
{
    class ConfigAudioTransfer:Config
    {
        public string[] InputAzureFolderPathArray { get; private set; }
        public string OutputAzureRootFolderPath { get; private set; } = @"D:\Tmp\Basel 18.11. 2020 S_Update";
        public string ExistringFileListPath { get; private set; } = @"D:\WorkFolder\Input\Summary.txt";
        public int SampleRate { get; private set; } = 16000;
        public int NumChannels { get; private set; } = 1;
        public string ReportRootFolderPath { get; private set; } = @"D:\Tmp";
        public string DailyRootFolderPath { get; private set; } = @"";
        public string AudioRootFolder { get; private set; } = @"D:\WorkFolder\Input\300hrsRecordingContent";
        protected override void LoadTaskNode()
        {
            string timeStamp = DateTime.Now.ToStringPathLong();

            InputAzureFolderPathArray = TaskNode.GetXmlValues("Input/AzureFolder", "Path").ToArray();
            OutputAzureRootFolderPath = TaskNode.GetXmlValue("OutputAzureRootFolder", "Path");
            
            SampleRate = TaskNode.GetXmlValueInt32("AudioSettings", "SampleRate");
            NumChannels = TaskNode.GetXmlValueInt32("AudioSettings", "NumChannels");

            ReportRootFolderPath = Path.Combine(TaskNode.GetXmlValue("ReportRootFolder", "Path"), "_Report", timeStamp);
            DailyRootFolderPath = Path.Combine(TaskNode.GetXmlValue("DailyRootFolder", "Path"), timeStamp);
            AudioRootFolder = TaskNode.GetXmlValue("AudioRootFolder", "Path");
        }
    }
}
