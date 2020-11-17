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
        protected XmlNode TaskNode = null;
        public Config() { }
        public void Load(string configPath)
        {
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(configPath);
            TaskName = xDoc.GetXmlValue("Root", "TaskName");
            TaskNode = xDoc["Root"][TaskName];
            LoadTaskNode();
        }

        protected virtual void LoadTaskNode()
        {

        }
    }
}
