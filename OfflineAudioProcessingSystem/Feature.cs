using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfflineAudioProcessingSystem
{
    abstract class Feature
    {        
        public void LoadAndRun(string configPath)
        {
            LoadConfig(configPath);
            Run();
        }
        public void TestRun()
        {
            Run();
        }
        protected abstract void LoadConfig(string configPath);
        protected abstract void Run();
    }
}
