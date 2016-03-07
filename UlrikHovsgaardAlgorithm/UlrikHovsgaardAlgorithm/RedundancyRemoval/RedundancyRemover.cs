using System.Collections.Generic;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    public static class RedundancyRemover
    {
        #region Fields
        
        private static readonly List<LogTrace> _inputUniqueTraces;
        private static UniqueTraceFinderWithComparison _uniqueTraceFinder = new UniqueTraceFinderWithComparison();
        private static DcrGraph _originalInputDcrGraph;
        private static DcrGraph _outputDcrGraph;

        // Redundancies
        public static HashSet<Activity> RedundantActivities { get; set; } = new HashSet<Activity>();

        #endregion
        
        #region Methods

        public static DcrGraph RemoveRedundancy(DcrGraph inputGraph)
        {
            _originalInputDcrGraph = inputGraph;
            _outputDcrGraph = _originalInputDcrGraph.Copy2();
            // Store the unique traces of original DcrGraph
            _uniqueTraceFinder.SupplyTracesToBeComparedTo(_uniqueTraceFinder.GetUniqueTraces(_originalInputDcrGraph));


            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant and is removed immediately
            // All the following calls potentially alter the OutputDcrGraph

            ReplaceRedundantRelations(RelationType.Responses);
            ReplaceRedundantRelations(RelationType.Conditions);
            ReplaceRedundantRelations(RelationType.InclusionsExclusions);
            ReplaceRedundantRelations(RelationType.Milestones);
            ReplaceRedundantRelations(RelationType.Deadlines);

            return _outputDcrGraph;
        }

        public enum RelationType { Responses, Conditions, Milestones, InclusionsExclusions, Deadlines }

        private static void ReplaceRedundantRelations(RelationType relationType)
        {
            // Determine method input
            Dictionary<Activity, HashSet<Activity>> relationDictionary = new Dictionary<Activity, HashSet<Activity>>();
            switch (relationType)
            {
                case RelationType.Responses:
                    relationDictionary = _originalInputDcrGraph.Responses;
                    break;
                case RelationType.Conditions:
                    relationDictionary = _originalInputDcrGraph.Conditions;
                    break;
                case RelationType.Milestones:
                    relationDictionary = _originalInputDcrGraph.Milestones;
                    break;
                case RelationType.InclusionsExclusions:
                    // Convert Dictionary<Activity, Dictionary<Activity, bool>> to Dictionary<Activity, HashSet<Activity>>
                    relationDictionary = ConvertToDictionaryActivityHashSetActivity(_originalInputDcrGraph.IncludeExcludes);
                    break;
                case RelationType.Deadlines:
                    // Convert Dictionary<Activity, Dictionary<Activity, TimeSpan>> to Dictionary<Activity, HashSet<Activity>>
                    relationDictionary = ConvertToDictionaryActivityHashSetActivity(_originalInputDcrGraph.Deadlines);
                    break;
            }

            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant
            foreach (var relation in relationDictionary)
            {
                var source = relation.Key;
                foreach (var target in relation.Value)
                {
                    var copy = _outputDcrGraph.Copy2(); // "Running copy"
                    // Attempt to remove the relation
                    switch (relationType)
                    {
                        case RelationType.Responses:
                            copy.Responses[source].Remove(target);
                            break;
                        case RelationType.Conditions:
                            copy.Conditions[source].Remove(target);
                            break;
                        case RelationType.Milestones:
                            copy.Milestones[source].Remove(target);
                            break;
                        case RelationType.InclusionsExclusions:
                            copy.IncludeExcludes[source].Remove(target);
                            break;
                        case RelationType.Deadlines:
                            copy.Deadlines[source].Remove(target);
                            break;
                    }

                    // Compare unique traces - if equal (true), relation is redundant
                    if (_uniqueTraceFinder.CompareTracesFoundWithSupplied(copy))
                    {
                        // The relation is redundant, replace running copy with current copy (with the relation removed)
                        _outputDcrGraph = copy;
                    }
                }
            }
        }

        private static Dictionary<Activity, HashSet<Activity>> ConvertToDictionaryActivityHashSetActivity<T>(Dictionary<Activity, Dictionary<Activity, T>> inputDictionary)
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

        #endregion
    }
}
