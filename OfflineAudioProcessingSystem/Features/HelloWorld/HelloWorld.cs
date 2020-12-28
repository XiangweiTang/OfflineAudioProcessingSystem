using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfflineAudioProcessingSystem.HelloWorld
{
    class HelloWorld:Feature
    {
        ConfigHelloWorld Cfg = new ConfigHelloWorld();
        protected override void LoadConfig(string configPath)
        {
            Cfg.Load(configPath);
        }

        protected override void Run()
        {
            Console.WriteLine($"Hello world {Cfg.Name}!");
        }
    }
}
