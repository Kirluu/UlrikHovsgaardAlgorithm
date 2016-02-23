using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class RedundancyRemover
    {
        #region Fields

        // Property fields
        private DcrGraph _originalInputDcrGraph;

        // Other fields
        private readonly List<LogTrace> _inputUniqueTraces;



        #endregion

        #region Properties

        // Input
        private DcrGraph OriginalInputDcrGraph
        {
            get { return _originalInputDcrGraph; }
            set
            {
                _originalInputDcrGraph = value;
                // Set initial redundancy properties
                RedundantActivities = _originalInputDcrGraph.Activities;
                RedundantResponses = _originalInputDcrGraph.Responses;
                RedundantIncludesExcludes = _originalInputDcrGraph.IncludeExcludes;
                RedundantConditions = _originalInputDcrGraph.Conditions;
                RedundantMilestones = _originalInputDcrGraph.Milestones;
                RedundantDeadlines = _originalInputDcrGraph.Deadlines;
            }
        }

        // Redundancies
        public HashSet<Activity> RedundantActivities { get; set; } = new HashSet<Activity>();
        public Dictionary<Activity, HashSet<Activity>> RedundantResponses { get; set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, Dictionary<Activity, bool>> RedundantIncludesExcludes { get; set; } = new Dictionary<Activity, Dictionary<Activity, bool>>(); // bool TRUE is include
        public Dictionary<Activity, HashSet<Activity>> RedundantConditions { get; set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> RedundantMilestones { get; set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, Dictionary<Activity, TimeSpan>> RedundantDeadlines { get; set; } = new Dictionary<Activity, Dictionary<Activity, TimeSpan>>();

        #endregion

        public RedundancyRemover(DcrGraph inputGraph)
        {
            OriginalInputDcrGraph = inputGraph;
            _inputUniqueTraces = new UniqueTraceFinder().GetUniqueTraces(OriginalInputDcrGraph);
        }

        #region Methods

        public DcrGraph RemoveRedundancy()
        {
            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant
            foreach (var response in OriginalInputDcrGraph.Responses)
            {
                var source = response.Key;
                foreach (var target in response.Value)
                {
                    var copy = OriginalInputDcrGraph.Copy();
                    copy.Responses[source].Remove(target);
                    
                    // Compare unique traces
                    var newUniqueTraces = new UniqueTraceFinder().GetUniqueTraces(copy);
                    
                }
                
            }

            return OriginalInputDcrGraph;
        }

        // TODO: Can be improved if both inputlists are sorted by event-ID
        private bool AreUniqueTracesEqual(List<LogTrace> traces1, List<LogTrace> traces2)
        {
            foreach (var trace1 in traces1)
            {
                bool matchingTraceFound;
                foreach (var trace2 in traces2)
                {
                    var diff1 = trace1.Events.Except(trace2.Events);
                    var diff2 = trace2.Events.Except(trace1.Events);
                    if (!diff1.Any() && !diff2.Any())
                    {
                        matchingTraceFound = true;
                        break;
                    }
                }
            }

            return false;
        }

        #endregion
    }
}
