using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfflineAudioProcessingSystem.AudioTransfer
{
    class ConfigAudioTransfer:Config
    {
        public string InputPath { get; private set; } = @"D:\Tmp\ParticipantData";
        public string OutputPath { get; private set; } = @"D:\Tmp\ParticipantData_Update";
        public int SampleRate { get; private set; } = 16000;
        public int NumChannels { get; private set; } = 1;
    }
}
