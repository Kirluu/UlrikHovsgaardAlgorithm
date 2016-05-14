using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.GraphSimulation;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    public class RedundancyRemover
    {
        public event Action<string> ReportProgress;
        
        #region Fields
        
        public UniqueTraceFinder UniqueTraceFinder { get; private set; }
        public HashSet<ComparableList<int>> OriginalGraphUniqueTraces { get; private set; }
        private DcrGraph _originalInputDcrGraph;
        private BackgroundWorker _worker;

        #endregion

        #region Properties

        public HashSet<Activity> RedundantActivities { get; set; } = new HashSet<Activity>();
        public DcrGraph OutputDcrGraph { get; private set; }

        #endregion

        #region Methods

        public DcrGraph RemoveRedundancy(DcrGraph inputGraph, BackgroundWorker worker = null)
        {
            _worker = worker;
#if DEBUG
            Console.WriteLine("Started redundancy removal:");
#endif

            //TODO: use an algorithm to check if the graph is connected and if not then recursively remove redundancy on the subgraphs.
            //temporarily remove flower activities. 
            var copy = inputGraph.Copy();

            var removedActivities =
                copy.GetActivities().Where(x => (x.Included && !copy.ActivityHasRelations(x))).ToList();

            foreach (var a in removedActivities)
            {
                copy.RemoveActivity(a.Id);
            }

            var byteDcrGraph = new ByteDcrGraph(copy);

            OriginalGraphUniqueTraces = UniqueTraceFinder.GetUniqueTraces(byteDcrGraph);

            //first we find all activities that are never mentioned (Using lookup in IndexToActivityId Dictionary)
            var notInTraces = copy.GetActivities().Where(x => UniqueTraceFinder.UniqueTraceSet.ToList().TrueForAll(y => y.TrueForAll(z => z != Int32.Parse(byteDcrGraph.ActivityIdToIndexId[x.Id])))).Select(x => x.Id).ToList();

            //and remove them and the relations they are involved
            foreach (var id in notInTraces)
            {
                copy.RemoveActivity(id);
            }

            _originalInputDcrGraph = copy.Copy();
            OutputDcrGraph = copy;

            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant and is removed immediately
            // All the following calls potentially alter the OutputDcrGraph

#if DEBUG
            Console.WriteLine("\nTesting " + _originalInputDcrGraph.Responses.Count + "*n Responce-relations: ");
#endif
            if (_worker?.CancellationPending == true) return _originalInputDcrGraph;
            RemoveRedundantRelations(RelationType.Response);

#if DEBUG
            Console.WriteLine("\nTesting " + _originalInputDcrGraph.Conditions.Count + "*n Condition-relations: ");
#endif
            if (_worker?.CancellationPending == true) return _originalInputDcrGraph;
            RemoveRedundantRelations(RelationType.Condition);

#if DEBUG
            Console.WriteLine("\nTesting " + _originalInputDcrGraph.IncludeExcludes.Count + "*n Include-exclude-relations: ");
#endif
            if (_worker?.CancellationPending == true) return _originalInputDcrGraph;
            RemoveRedundantRelations(RelationType.InclusionExclusion);

#if DEBUG
            Console.WriteLine("\nTesting " + _originalInputDcrGraph.Milestones.Count + "*n Milestone-relations: ");
#endif
            if (_worker?.CancellationPending == true) return _originalInputDcrGraph;
            RemoveRedundantRelations(RelationType.Milestone);

            foreach (var a in removedActivities)
            {
                OutputDcrGraph.AddActivity(a.Id,a.Name);
                OutputDcrGraph.SetIncluded(true,a.Id);
                OutputDcrGraph.SetPending(a.Pending,a.Id);
            }
            var nested = OutputDcrGraph.ExportToXml();

            return OutputDcrGraph;
        }

        public enum RelationType { Response, Condition, Milestone, InclusionExclusion}

        private void RemoveRedundantRelations(RelationType relationType)
        {
            // Determine method input
            Dictionary<Activity, HashSet<Activity>> relationDictionary = new Dictionary<Activity, HashSet<Activity>>();
            switch (relationType)
            {
                case RelationType.Response:
                    relationDictionary = _originalInputDcrGraph.Responses;
                    break;
                case RelationType.Condition:
                    relationDictionary = _originalInputDcrGraph.Conditions;
                    break;
                case RelationType.Milestone:
                    relationDictionary = _originalInputDcrGraph.Milestones;
                    break;
                case RelationType.InclusionExclusion:
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
                    if (_worker?.CancellationPending == true) return;
#if DEBUG
                    Console.WriteLine("Removing " + relationType + " from " + source.Id + " to " + target.Id + ":");
#endif
                    ReportProgress?.Invoke("Removing " + relationType + " from " + source.Id + " to " + target.Id);

                    var copy = OutputDcrGraph.Copy(); // "Running copy"
                    var retrievedTarget = copy.GetActivity(target.Id);
                    // Attempt to remove the relation
                    switch (relationType)
                    {
                        case RelationType.Response:
                            copy.Responses[copy.GetActivity(source.Id)].Remove(retrievedTarget);
                            break;
                        case RelationType.Condition:
                            copy.Conditions[copy.GetActivity(source.Id)].Remove(retrievedTarget);
                            break;
                        case RelationType.Milestone:
                            copy.Milestones[copy.GetActivity(source.Id)].Remove(retrievedTarget);
                            break;
                        case RelationType.InclusionExclusion:
                            if (source.Id == target.Id) // Assume self-exclude @ equal IDs (Assumption that relation-addition METHODS in DcrGraph have been used to add relations)
                            {
                                continue; // ASSUMPTION: A self-exclude on an activity that is included at some point is never redundant
                                // Recall: All never-included activities have already been removed from graph
                            }
                            copy.IncludeExcludes[copy.GetActivity(source.Id)].Remove(retrievedTarget);
                            break;
                    }

                    //var ut2 = new UniqueTraceFinder(new ByteDcrGraph(copy));

                    // Compare unique traces - if equal (true), relation is redundant
                    //if (CompareTraceSet(UniqueTraceFinder.UniqueTraceSet, ut2.UniqueTraceSet))
                    if (UniqueTraceFinder.CompareTraces(new ByteDcrGraph(copy), OriginalGraphUniqueTraces))
                    {
                        // The relation is redundant, replace running copy with current copy (with the relation removed)
                        OutputDcrGraph = copy;
                    }
                }
            }
        }

        private bool CompareTraceSet(HashSet<List<int>> me, HashSet<List<int>> you)
        {
            if (me.Count != you.Count)
            {
                return false;
            }

            //could prob be optimized
            foreach (var list in me)
            {
                you.RemoveWhere(l => CompareTraces(l, list));
            }

            return !you.Any();

        }

        private bool CompareTraces(List<int> me, List<int> you)
        {
            if (me.Count != you.Count)
            {
                return false;
            }
            
            return !me.Where((t, i) => t != you[i]).Any();
        }

        #endregion
    }
}
