using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Common;

namespace OfflineAudioProcessingSystem
{
    class Config
    {
        public string TaskName { get; private set; } = "NA";
        public string SoxPath { get; private set; } = @"C:\Program Files (x86)\sox-14-4-2\sox.exe";
        public string FfmpegPath { get; private set; } = @"D:\AutomationSystem\ExternalTools\ffmpeg.exe";
        protected XmlNode TaskNode = null;
        private XmlNode CommonNode = null;
        public Config() { }
        public void Load(string configPath)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(configPath);
            TaskName = xDoc.GetXmlValue("Root", "TaskName");
            TaskNode = xDoc["Root"][TaskName];
            LoadTaskNode();
            CommonNode = xDoc["Root"]["Common"];
            LoadCommonNode();
        }

        protected virtual void LoadTaskNode()
        {

        }

        private void LoadCommonNode()
        {
            SoxPath = CommonNode.GetXmlValue("Sox", "Path");
            FfmpegPath = CommonNode.GetXmlValue("Ffmpeg", "Path");
        }
    }
}
