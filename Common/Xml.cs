using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Common
{
    public static class Xml
    {
        public static string GetXmlValue(this XmlNode rootNode, string xpath, string attribute = "")
        {
            Sanity.Requires(rootNode != null, "The root node is null.");
            var subNode = string.IsNullOrEmpty(xpath) ? rootNode : rootNode.SelectSingleNode(xpath);
            Sanity.Requires(subNode != null, $"The xpath {xpath} doesn't exist.");
            return string.IsNullOrEmpty(attribute)
                ? subNode.InnerText
                : subNode.Attributes[attribute].Value;
        }

        public static int GetXmlValueInt32(this XmlNode rootNode, string xpath, string attribute = "")
        {
            string s = rootNode.GetXmlValue(xpath, attribute);
            return int.Parse(s);
        }
    }
}
