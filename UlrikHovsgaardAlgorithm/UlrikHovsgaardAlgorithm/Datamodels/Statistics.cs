using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithm.Datamodels
{
    public class RedundancyStatistics
    {
        /// <summary>
        /// The redundant activities and/or relations removed.
        /// </summary>
        public List<RedundancyEvent> RedundancyCount { get; set; }

        /// <summary>
        /// The combined time spent.
        /// </summary>
        public TimeSpan TimeSpent { get; set; }
    }

    public struct PatternAlgorithmResult
    {
        public List<RedundancyEvent> Redundancies { get; set; }
        public Dictionary<string, (List<RedundancyEvent>, TimeSpan)> PatternStatistics { get; set; }
        public int RoundsSpent { get; set; }
    }
}
