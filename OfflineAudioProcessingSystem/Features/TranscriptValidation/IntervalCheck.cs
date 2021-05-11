using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace OfflineAudioProcessingSystem.TranscriptValidation
{
    static class IntervalCheck
    {
        public static double INTERVAL_OVERLAP_THRESHOLD { get; set; } = 1;
        public static double INTERVAL_MISMATCH_THRESHOLD { get; set; } = 1;
        public static (List<(double start, double end)> collapsedIntervals, bool valid ) MergeIntervals(IEnumerable<(double start,double end)> intervalSequence)
        {
            List<(double start, double end)> collapsedIntervals = new List<(double start, double end)>();
            bool valid = true;
            var orderedSeq = intervalSequence.OrderBy(x => x.start);
            foreach(var currentInterval in orderedSeq)
            {
                if (currentInterval.end <= currentInterval.start)
                {
                    continue;
                }
                if(collapsedIntervals.Count==0)
                {
                    collapsedIntervals.Add(currentInterval);
                    continue;
                }
                var lastInterval = collapsedIntervals[collapsedIntervals.Count - 1];
                if (lastInterval.end >= currentInterval.end)
                {
                    valid = false;
                    continue;
                }
                if (lastInterval.end <= currentInterval.start)
                {
                    collapsedIntervals.Add(currentInterval);
                    continue;
                }
                if (lastInterval.end > currentInterval.start)
                {
                    valid = lastInterval.end - currentInterval.start <= INTERVAL_OVERLAP_THRESHOLD;
                    collapsedIntervals[collapsedIntervals.Count - 1] = (lastInterval.start, currentInterval.end);
                    continue;
                }
                Sanity.Throw("Invalid sequnce.");
            }
            return (collapsedIntervals, valid);
        }

        public static double CalculateIntervalMiss(IList<(double start, double end)> collapsedSeq, IList<(double start, double end)> validSeq)
        {
            int collapsedIndex = 0;
            int validIndex = 0;
            double preOverlap = 0;
            double totalMiss = 0;
            while (collapsedIndex < collapsedSeq.Count && validIndex < validSeq.Count)
            {
                var currentValid=validSeq[validIndex];
                var currentCollapsed = collapsedSeq[collapsedIndex];
                double currentValidLength = currentValid.end - currentValid.start;

                if (currentValid.start >= currentCollapsed.end)
                {
                    // In this case, the current valid is completely after the current collapsed.
                    // Do nothing, move the collapsed forward.
                    collapsedIndex++;
                    continue;
                }
                if (currentValid.end <= currentCollapsed.start)
                {
                    // In this case, the current valid is completely before the current collapsed.
                    // The current valid may overlap with the previous one, may not.
                    // So the missed part should be the current valid interval's length minus the pre overlap.
                    totalMiss += currentValidLength - preOverlap;
                    // Since no overlap in the current pair, reset this to 0.
                    preOverlap = 0;
                    // We may check the next valid one(if there are any).
                    validIndex++;
                    continue;
                }
                if (currentValid.start >= currentCollapsed.start && currentValid.end <= currentCollapsed.end)
                {
                    // In this case, the current valid is completely inside the current collapsed.
                    // Do nothing, move the valid forward.
                    validIndex++;
                    continue;
                }

                if (currentValid.start <= currentCollapsed.start && currentValid.end >= currentCollapsed.end)
                {
                    // In this case, the current valid covers the current collapsed.
                    // The overlap part should minus the current collapsed part.
                    preOverlap += currentCollapsed.end - currentCollapsed.start;
                    // Move the collapsed forward.
                    collapsedIndex++;
                    continue;
                }
                

                if (currentValid.start <= currentCollapsed.start && currentValid.end <= currentCollapsed.end)
                {
                    // In this case, the current valid is overall prior than the current collapsed one.
                    // The current valid may overlap with the previous one, may not.
                    // So the missed part should be the current valid minus pre overlap, minus current overlap.
                    totalMiss += currentValidLength - preOverlap - (currentValid.end - currentCollapsed.start);
                    // Since the current overlap will not affect the next one, reset this to 0.
                    preOverlap = 0;
                    // We may check the next valid one(if there are any).
                    validIndex++;
                    continue;
                }

                if (currentValid.start >= currentCollapsed.start && currentValid.end >= currentCollapsed.end)
                {
                    // In this case, the current valid is overall after the current collapsed one.
                    // So we need to udpate the pre overlap, since the valid index will not change.
                    preOverlap = currentCollapsed.end - currentValid.start;
                    // The current collapsed is done.
                    collapsedIndex++;
                    continue;
                }


                Sanity.Throw("Invalid sequences.");
            }

            while (validIndex < validSeq.Count)
            {                
                totalMiss += validSeq[validIndex].end - validSeq[validIndex].start - preOverlap;
                preOverlap = 0;
                validIndex++;
            }

            return totalMiss;
        }
    }
}
