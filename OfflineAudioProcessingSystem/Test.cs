using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;

namespace OfflineAudioProcessingSystem
{
    class Test
    {
        public Test(string[] args)
        {
            AudioTransfer.AudioTransfer at = new AudioTransfer.AudioTransfer();
            at.TestRun();
        }
    }
}
