using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace OfflineAudioProcessingSystem
{
    class Test
    {
        public Test(string[] args)
        {
            string path = @"D:\Music\Aimer\Dawn\LAST STARDUST.wav";
            Wave w = new Wave();
            w.ShallowParse(path);
        }
    }
}
