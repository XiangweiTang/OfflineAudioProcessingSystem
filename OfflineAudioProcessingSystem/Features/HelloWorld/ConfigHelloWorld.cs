using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace OfflineAudioProcessingSystem.HelloWorld
{
    class ConfigHelloWorld:Config
    {
        public string Name { get; private set; }
        protected override void LoadTaskNode()
        {
            Name = TaskNode.GetXmlValue("Name");
        }
    }
}
