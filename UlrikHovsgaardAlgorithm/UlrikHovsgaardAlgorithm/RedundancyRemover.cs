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
        private DcrGraph _inputDcrGraph;

        // Other fields
        private List<LogTrace> _uniqueTraces = new List<LogTrace>();



        #endregion

        #region Properties

        // Input
        private DcrGraph InputDcrGraph
        {
            get { return _inputDcrGraph; }
            set
            {
                _inputDcrGraph = value;
                // Set initial redundancy properties
                RedundantActivities = _inputDcrGraph.Activities;
                RedundantResponses = _inputDcrGraph.Responses;
                RedundantIncludesExcludes = _inputDcrGraph.IncludeExcludes;
                RedundantConditions = _inputDcrGraph.Conditions;
                RedundantMilestones = _inputDcrGraph.Milestones;
                RedundantDeadlines = _inputDcrGraph.Deadlines;
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
            InputDcrGraph = inputGraph;
        }

        #region Methods

        public DcrGraph RemoveRedundancy()
        {

            return InputDcrGraph;
        }

        public void FindUniqueTraces()
        {


            // TODO: Use DCR graph and find all possible unique traces
            // TODO: Consider cycles...
            // TODO: Consider brute-force (unique traces returned from LogGenerator9001) vs. methodical (Copy DCR-graph at each decision breakpoint, detect cyclic behavior)
        }

        #endregion
    }
}
