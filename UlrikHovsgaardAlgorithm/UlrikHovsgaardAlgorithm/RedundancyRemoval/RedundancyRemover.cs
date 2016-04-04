using System;
using System.Collections.Generic;
using System.Linq;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.GraphSimulation;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    public static class RedundancyRemover
    {
        #region Fields
        
        private static UniqueTraceFinderWithComparison _uniqueTraceFinder;
        private static DcrGraph _originalInputDcrGraph;
        private static DcrGraph _outputDcrGraph;
        
        // Redundancies
        public static HashSet<Activity> RedundantActivities { get; set; } = new HashSet<Activity>();

        #endregion

        #region Methods

        public static DcrGraph RemoveRedundancy(DcrGraph inputGraph)
        {
            //TODO: use an algorithm to check if the graph is connected and if not then recursively remove redundancy on the subgraphs.
            //temporarily remove flower activities. TODO: use enums for christ sake
            var removedActivities = 
                inputGraph.Activities.Where(x => (x.Included && !inputGraph.ActivityHasRelations(x))).ToList();

            foreach (var a in removedActivities)
            {
                inputGraph.RemoveActivity(a.Id);
            }

            _uniqueTraceFinder = new UniqueTraceFinderWithComparison(inputGraph);
            
            //first we find all activities that are never mentioned
            var notInTraces = inputGraph.Activities.Where(x => _uniqueTraceFinder.TracesToBeComparedToSet.ToList().TrueForAll(y => y.Events.TrueForAll(z => z.IdOfActivity != x.Id))).Select(x => x.Id).ToList();

            //and remove them and the relations they are involved
            foreach (var id in notInTraces)
            {
                inputGraph.RemoveActivity(id);
            }

            _originalInputDcrGraph = inputGraph;
            _outputDcrGraph = _originalInputDcrGraph.Copy();

            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant and is removed immediately
            // All the following calls potentially alter the OutputDcrGraph

            RemoveRedundantRelations(RelationType.Responses);
            RemoveRedundantRelations(RelationType.Conditions);
            RemoveRedundantRelations(RelationType.InclusionsExclusions);
            RemoveRedundantRelations(RelationType.Milestones);

            foreach (var a in removedActivities)
            {
                _outputDcrGraph.AddActivity(a.Id,a.Name);
                _outputDcrGraph.SetIncluded(true,a.Id);
                _outputDcrGraph.SetPending(a.Pending,a.Id);
            }

            return _outputDcrGraph;
        }

        public enum RelationType { Responses, Conditions, Milestones, InclusionsExclusions}

        private static void RemoveRedundantRelations(RelationType relationType)
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
                    relationDictionary = DcrGraph.ConvertToDictionaryActivityHashSetActivity(_originalInputDcrGraph.IncludeExcludes);
                    break;
            }

            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant
            foreach (var relation in relationDictionary)
            {
                var source = relation.Key;

                foreach (var target in relation.Value)
                {
#if DEBUG
                    Console.WriteLine("Removing " + relationType + " from " + source.Id + " to " + target.Id + ":");
#endif

                    var copy = _outputDcrGraph.Copy(); // "Running copy"
                    var retrievedTarget = copy.GetActivity(target.Id);
                    // Attempt to remove the relation
                    switch (relationType)
                    {
                        case RelationType.Responses:
                            copy.Responses[copy.GetActivity(source.Id)].Remove(retrievedTarget);
                            break;
                        case RelationType.Conditions:
                            copy.Conditions[copy.GetActivity(source.Id)].Remove(retrievedTarget);
                            break;
                        case RelationType.Milestones:
                            copy.Milestones[copy.GetActivity(source.Id)].Remove(retrievedTarget);
                            break;
                        case RelationType.InclusionsExclusions:
                            if (source.Id == target.Id) // Assume self-exclude @ equal IDs (Assumption that relation-addition METHODS in DcrGraph have been used to add relations)
                            {
                                continue; // ASSUMPTION: A self-exclude on an activity that is included at some point is never redundant
                                // Recall: All never-included activities have already been removed from graph
                            }
                            copy.IncludeExcludes[copy.GetActivity(source.Id)].Remove(retrievedTarget);
                            break;
                    }

                    // Compare unique traces - if equal (true), relation is redundant
                    if (_uniqueTraceFinder.CompareTracesFoundWithSuppliedThreaded(copy))
                    {
                        // The relation is redundant, replace running copy with current copy (with the relation removed)
                        _outputDcrGraph = copy;
                    }
                }
            }
        }

        #endregion
    }
}
