using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace OfflineAudioProcessingSystem.AudioTransfer
{
    class ConfigAudioTransfer:Config
    {
        public string InputPath { get; private set; } = @"D:\Tmp\Basel 18.11. 2020 S";
        public string OutputPath { get; private set; } = @"D:\Tmp\Basel 18.11. 2020 S_Update";
        public string ReportPath { get; private set; } = @"D:\Tmp\Basel 18.11. 2020 S.txt";
        public string ExistringFileListPath { get; private set; } = @"D:\WorkFolder\Input\Summary.txt";
        public string ErrorPath { get; private set; } = @"";
        public int SampleRate { get; private set; } = 16000;
        public int NumChannels { get; private set; } = 1;
        protected override void LoadTaskNode()
        {
            InputPath = TaskNode.GetXmlValue("InputFolder", "Path");
            OutputPath = System.IO.Path.Combine(TaskNode.GetXmlValue("OutputFolder", "Path"), InputPath.TrimEnd('\\').Split('\\').Last());
            ReportPath = InputPath + ".txt";
            ErrorPath = InputPath + ".error.txt";
            SampleRate = TaskNode.GetXmlValueInt32("AudioSettings", "SampleRate");
            NumChannels = TaskNode.GetXmlValueInt32("AudioSettings", "NumChannels");
        }
    }
}
