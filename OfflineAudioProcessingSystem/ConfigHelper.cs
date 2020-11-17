using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.IO;

namespace OfflineAudioProcessingSystem
{
    class ConfigHelper
    {
        public ConfigHelper() { }
        public void Run(string[] args)
        {
            if (args.Length == 0)
            {
                RunXmlMod("Config.xml");
            }
            else if (args[0] == "magictest")
            {
                _ = new Test(args.Skip(1).ToArray());
            }
            else
            {
                RunXmlMod(args[0]);
            }
        }

        private void RunXmlMod(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                    PrintConfig();
                Config cfg = new Config();
                cfg.Load(configPath);
                Init(cfg);
                var feature = GetFeature(cfg.TaskName);
                if (feature != null)
                    feature.LoadAndRun(configPath);
            }
            catch (CommonException e)
            {
                Logger.WriteLine(e.Message, true, true);
            }
        }

        private void Init(Config cfg)
        {
            LocalCommon.SoxPath = cfg.SoxPath;
            LocalCommon.FfmpegPath = cfg.FfmpegPath;
        }

        private Feature GetFeature(string featureName)
        {
            switch (featureName.ToLower())
            {
                case "na":
                    return null;
                case "helloworld":
                    return new HelloWorld.HelloWorld();
                default:
                    throw new CommonException($"Invalid task name {featureName}.");
            }
        }

        private void PrintConfig()
        {
            var list = IO.ReadEmbed($"{LocalConstants.LOCAL_ASMB_NAME}.Config.xml", LocalConstants.LOCAL_ASMB_NAME);
            File.WriteAllLines("Config.xml", list);
        }
    }
}
