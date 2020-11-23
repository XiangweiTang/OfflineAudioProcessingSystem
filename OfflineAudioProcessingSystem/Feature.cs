using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Common;

namespace OfflineAudioProcessingSystem
{
    abstract class Feature
    {        
        private string WorkFolder = "";        
        public void LoadAndRun(string configPath)
        {
            Preset();
            LoadConfig(configPath);
            Run();
        }
        public void TestRun()
        {
            Run();
        }
        public abstract string GetFeatureName();
        protected abstract void LoadConfig(string configPath);
        protected abstract void Run();

        private void Preset()
        {
            WorkFolder = Path.Combine("Tmp", $"{DateTime.Now.ToStringPathLong()}_{GetFeatureName()}");            
            Directory.CreateDirectory(WorkFolder);

            Logger.LogPath = Path.Combine(WorkFolder, "Log.txt");
            Logger.ErrorPath = Path.Combine(WorkFolder, "Error.txt");
        }
    }
}
