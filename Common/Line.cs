using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public abstract class Line
    {
        public char Sep { get; set; } = '\t';        
        public string OriginalString { get; }
        public Line() { }
        public Line(string lineStr)
        {
            OriginalString = lineStr;
            var split = lineStr.Split(Sep);
            SetLine(split);
        }
        public string Output()
        {
            return string.Join(Sep.ToString(), GetLine());
        }
        protected abstract void SetLine(string[] split);
        protected abstract IEnumerable<object> GetLine();
    }
}
