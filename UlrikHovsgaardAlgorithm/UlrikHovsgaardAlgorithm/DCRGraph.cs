using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public enum InclusionOrExclusion { Inclusion, Exclusion }

    public class DCRGraph
    {
        public HashSet<Activity> Alphabet { get; set; }
        
        public Dictionary<Activity, Dictionary<Activity, InclusionOrExclusion>> InclusionsAndExclusions { get; set; }
        public Dictionary<Activity, HashSet<Activity>> Preconditions { get; set; }
        public Dictionary<Activity, HashSet<Activity>> Responses { get; set; }
        public Dictionary<Activity, HashSet<Activity>> Milestones { get; set; }
        public Dictionary<Activity, Dictionary<Activity, TimeSpan>> Deadlines { get; set; }
    }
}
