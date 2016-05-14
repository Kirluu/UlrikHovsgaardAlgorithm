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

        private UniqueTraceFinder _uniqueTraceFinder;
        public HashSet<ComparableList<int>> OriginalGraphUniqueTraces { get; private set; }
        private DcrGraph _originalInputDcrGraph;
        private BackgroundWorker _worker;
        #endregion

        #region Properties
        
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


            _uniqueTraceFinder = new UniqueTraceFinder(byteDcrGraph);
            
            _originalInputDcrGraph = copy.Copy();
            OutputDcrGraph = copy;


            // Remove relations and see if the unique traces acquired are the same as the original. If so, the relation is clearly redundant and is removed immediately
            // All the following calls potentially alter the OutputDcrGraph
            
            RemoveRedundantRelations(RelationType.Response);
            
            RemoveRedundantRelations(RelationType.Condition);
            
            RemoveRedundantRelations(RelationType.InclusionExclusion);
            
            RemoveRedundantRelations(RelationType.Milestone);



            foreach (var activity in OutputDcrGraph.GetActivities())
            {
                var graphCopy = new ByteDcrGraph(byteDcrGraph);

                graphCopy.RemoveActivity(activity.Id);
                
                ReportProgress?.Invoke("Removing Activity " + activity.Id);

                // Compare unique traces - if equal activity is redundant
                if (_uniqueTraceFinder.CompareTraces(graphCopy))
                {
                    // The relation is redundant, replace  copy with current copy (with the relation removed)
                    OutputDcrGraph.RemoveActivity(activity.Id);
                }

            }


            foreach (var a in removedActivities)
            {
                OutputDcrGraph.AddActivity(a.Id, a.Name);
                OutputDcrGraph.SetIncluded(true, a.Id);
                OutputDcrGraph.SetPending(a.Pending, a.Id);
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
                    
                    // Compare unique traces - if equal (true), relation is redundant
                    //if (CompareTraceSet(UniqueTraceFinder.UniqueTraceSet, ut2.UniqueTraceSet))
                    if (_uniqueTraceFinder.CompareTraces(new ByteDcrGraph(copy)))
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
