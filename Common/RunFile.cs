using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Common
{
    public static class RunFile
    {
        public static void Run(string fileName, string arguments, bool createNewWindow, string workingDirectory)
        {
            Sanity.Requires(File.Exists(fileName), "Missing file run file.");
            using(Process proc=new Process())
            {
                proc.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments=arguments,
                    UseShellExecute = createNewWindow,
                    WorkingDirectory = workingDirectory,
                };
                proc.Start();
                proc.WaitForExit();
            }
        }
    }
}
