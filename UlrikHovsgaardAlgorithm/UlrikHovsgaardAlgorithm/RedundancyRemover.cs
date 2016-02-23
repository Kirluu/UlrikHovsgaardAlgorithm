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
        
        private readonly List<LogTrace> _inputUniqueTraces;

        #endregion

        #region Properties

        // Input
        private DcrGraph OriginalInputDcrGraph { get; }
        private DcrGraph OutputDcrGraph { get; set; }

        // Redundancies
        public HashSet<Activity> RedundantActivities { get; set; } = new HashSet<Activity>();

        #endregion

        public RedundancyRemover(DcrGraph inputGraph)
        {
            OriginalInputDcrGraph = inputGraph;
            OutputDcrGraph = OriginalInputDcrGraph.Copy();
            _inputUniqueTraces = new UniqueTraceFinder().GetUniqueTraces(OriginalInputDcrGraph);
        }

        #region Methods

        public DcrGraph RemoveRedundancy()
        {
            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant and is removed immediately
            // All the following calls potentially alter the OutputDcrGraph

            ReplaceRedundantRelations(RelationType.Responses);
            ReplaceRedundantRelations(RelationType.Conditions);
            ReplaceRedundantRelations(RelationType.InclusionsExclusions);
            ReplaceRedundantRelations(RelationType.Milestones);

            return OutputDcrGraph;
        }

        private enum RelationType { Responses, Conditions, Milestones, InclusionsExclusions, Deadlines }

        private void ReplaceRedundantRelations(RelationType relationType)
        {
            // Determine method input
            Dictionary<Activity, HashSet<Activity>> relationDictionary = new Dictionary<Activity, HashSet<Activity>>();
            switch (relationType)
            {
                case RelationType.Responses:
                    relationDictionary = OriginalInputDcrGraph.Responses;
                    break;
                case RelationType.Conditions:
                    relationDictionary = OriginalInputDcrGraph.Conditions;
                    break;
                case RelationType.Milestones:
                    relationDictionary = OriginalInputDcrGraph.Milestones;
                    break;
                case RelationType.InclusionsExclusions:
                    // Convert Dictionary<Activity, Dictionary<Activity, bool>> to Dictionary<Activity, HashSet<Activity>>
                    relationDictionary = ConvertToDictionaryActivityHashSetActivity(OriginalInputDcrGraph.IncludeExcludes);
                    break;
                case RelationType.Deadlines:
                    // Convert Dictionary<Activity, Dictionary<Activity, TimeSpan>> to Dictionary<Activity, HashSet<Activity>>
                    relationDictionary = ConvertToDictionaryActivityHashSetActivity(OriginalInputDcrGraph.Deadlines);
                    break;
            }

            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant
            foreach (var relation in relationDictionary)
            {
                var source = relation.Key;
                foreach (var target in relation.Value)
                {
                    var copy = OutputDcrGraph.Copy(); // "Running copy"
                    // Attempt to remove the relation
                    copy.Responses[source].Remove(target);

                    // Compare unique traces
                    if (AreUniqueTracesEqual(_inputUniqueTraces, new UniqueTraceFinder().GetUniqueTraces(copy)))
                    {
                        // The relation is redundant, replace running copy with current copy (with the relation removed)
                        OutputDcrGraph = copy;
                    }
                }
            }
        }

        private Dictionary<Activity, HashSet<Activity>> ConvertToDictionaryActivityHashSetActivity<T>(Dictionary<Activity, Dictionary<Activity, T>> inputDictionary)
        {
            var resultDictionary = new Dictionary<Activity, HashSet<Activity>>();
            foreach (var includeExclude in inputDictionary)
            {
                var source = includeExclude.Key;
                foreach (var keyValuePair in includeExclude.Value)
                {
                    var target = keyValuePair.Key; // .Value is a bool value which isn't used in the returned Dictionary
                    HashSet<Activity> targets;
                    resultDictionary.TryGetValue(source, out targets);
                    if (targets == null)
                    {
                        resultDictionary.Add(source, new HashSet<Activity> { target });
                    }
                    else
                    {
                        resultDictionary[source].Add(target);
                    }
                }
            }
            return resultDictionary;
        }

        // TODO: Can be improved if both inputlists are sorted by event-ID
        private bool AreUniqueTracesEqual(List<LogTrace> traces1, List<LogTrace> traces2)
        {
            traces1.Sort();
            traces2.Sort();

            foreach (var trace1 in traces1)
            {
                bool matchingTraceFound = false;
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
                if (!matchingTraceFound)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
