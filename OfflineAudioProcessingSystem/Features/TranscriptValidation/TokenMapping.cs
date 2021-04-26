using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace OfflineAudioProcessingSystem.TranscriptValidation
{
    class TokenMapping
    {
        static readonly char[] Sep = { ' ' };
        public (string oContent,string cContent) MappingTokens(string o, string c, int errorCode)
        {
            string compareO = o;
            string compareC = c;
            var oTokens = o.Split(Sep,StringSplitOptions.RemoveEmptyEntries);
            var cTokens = c.Split(Sep,StringSplitOptions.RemoveEmptyEntries);
            if (o.Last() == '>' && c.Last() != '>')
            {
                string oLast = oTokens.Last();
                c = $"{c} {oLast}";
            }
            if (c.Last() == '>' && o.Last() != '>')
            {
                string cLast = cTokens.Last();
                o = $"{o} {cLast}";
            }

            if (oTokens.Last()[0] == '<' && oTokens.Last() != cTokens.Last())
            {
                cTokens[cTokens.Length - 1] = oTokens.Last();
            }

            var oTagArray = oTokens.Where(x => x[0] == '<').ToArray();
            var cTagArray = cTokens.Where(x => x[0] == '<').ToArray();
            if (oTagArray.SequenceEqual(cTagArray))
            {
                return (
                    string.Join(" ", oTokens),
                    string.Join(" ", cTokens)
                    );
            }
            MinimumEditDistance<string>.RunWithBackTrack(oTagArray, cTagArray);
            var seq = MinimumEditDistance<string>.BackTrack("").ToArray();
            Sanity.Requires(MinimumEditDistance<string>.IsUnique, errorCode);

            List<string> oTagList = new List<string>();
            List<string> cTagList = new List<string>();
            foreach(var item in seq.Reverse())
            {
                if (item.Item1 == item.Item2)
                {
                    oTagList.Add(item.Item1);
                    cTagList.Add(item.Item2);
                    continue;
                }
                if (item.Item1 == "")
                {
                    cTagList.Add("");
                    continue;
                }
                if (item.Item2 == "")
                {
                    oTagList.Add("");
                    continue;
                }
                oTagList.Add(item.Item1);
                cTagList.Add(item.Item1);
            }
            if (oTagList.Count != oTagArray.Length || cTagList.Count != cTagArray.Length)
                ;
            Sanity.Requires(oTagList.Count == oTagArray.Length, errorCode);
            Sanity.Requires(cTagList.Count == cTagArray.Length, errorCode);

            string newO = Merge(oTokens, oTagList);
            string newC = Merge(cTokens, cTagList);
            return (newO, newC);
        }

        private string Merge(IList<string> tokens, IList<string> tags)
        {
            int j = 0;
            for(int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i][0] == '<')
                {
                    Sanity.Requires(j < tags.Count);
                    tokens[i] = tags[j];
                    j++;                    
                }
            }
            Sanity.Requires(j == tags.Count);
            return string.Join(" ", tokens.Where(x => x != ""));
        }
    }
}
