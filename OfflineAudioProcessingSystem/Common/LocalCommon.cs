using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace OfflineAudioProcessingSystem
{
    public static class LocalCommon
    {
        public static void RunFfmpeg(string arguments)
        {
            RunFile.Run(LocalConstants.FFMPEG_PATH, arguments, false, "");
        }
        public static void RunSox(string arguments)
        {
            RunFile.Run(LocalConstants.SOX_PATH, arguments, false, "");
        }
    }
}
