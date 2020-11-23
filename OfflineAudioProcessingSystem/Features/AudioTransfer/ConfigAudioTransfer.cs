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
        public int SampleRate { get; private set; } = 16000;
        public int NumChannels { get; private set; } = 1;
        protected override void LoadTaskNode()
        {
            InputPath = TaskNode.GetXmlValue("InputFolder", "Path");
            OutputPath = TaskNode.GetXmlValue("OutputFolder", "Path");
            ReportPath = TaskNode.GetXmlValue("ReportFile", "Path");
            SampleRate = TaskNode.GetXmlValueInt32("AudioSettings", "SampleRate");
            NumChannels = TaskNode.GetXmlValueInt32("AudioSettings", "NumChannels");
        }
    }
}
