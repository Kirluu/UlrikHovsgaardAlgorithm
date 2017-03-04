using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithm.Mining
{
    /// <summary>
    /// testing sutff
    /// </summary>
    public class ContradictionApproach
    {
        public static event Action<DcrGraph> PostProcessingResultEvent;

        //This is the mined graph. NOT THE ACTUAL RUNNING GRAPH.
        public DcrGraph Graph = new DcrGraph();
        private Queue<Activity> _run = new Queue<Activity>();
        private string _runId;
        private readonly Dictionary<string, Queue<Activity>> _allRuns = new Dictionary<string, Queue<Activity>>();

        private Activity _last;
        private const int MinimumNestedSize = 3;

        public ContradictionApproach(HashSet<Activity> activities)
        {
            //initialising activities
            foreach (var a in activities)
            {
                Graph.AddActivity(a.Id, a.Name, a.Roles);
                //a is excluded
                Graph.SetIncluded(false, a.Id);
                //a is Pending
                Graph.SetPending(true, a.Id);
            }
            foreach (var a1 in activities)
            {
                foreach (var a2 in activities)
                {
                    //add exclude from everything to everything
                    Graph.AddIncludeExclude(false,a1.Id,a2.Id);
                    Graph.AddCondition(a1.Id, a2.Id);
                    Graph.AddResponse(a1.Id, a2.Id);
                }
            }

            //_included = Graph.GetIncludedActivities();
        }

        public bool AddEvent(string id, string instanceId)
        {
            if (instanceId != _runId)
            { // add the currentRun to dictionary, if not the one we want to work on.
                if(_runId != null)
                    _allRuns[_runId] = _run;
                if (_allRuns.TryGetValue(instanceId, out _run))
                { //get the one we want to work on.
                    _runId = instanceId;
                    _last = _run.Peek();
                }
                else
                { 
                    _run = new Queue<Activity>();
                    _runId = instanceId;
                }
            }

            bool graphAltered;
            Activity currentActivity = Graph.GetActivity(id);
            if (_run.Count == 0)
            {
                currentActivity.Included = true;
                graphAltered = true;

            }
            else
            {
                //last activity now includes the current activity
                graphAltered = Graph.AddIncludeExclude(true, _last.Id, currentActivity.Id);
            }

            //remove ingoing conditions that has not been run before in the current Run.
            //the activities that has the activity as a condition target.
            var conditions = new HashSet<Activity>(Graph.Conditions.Where(a => a.Value.Contains(currentActivity)).Select(a => a.Key));

            //if it has no ingoing condition relations; continue.
            if (conditions.Count > 0)
            {
                //if an element is in conditions and not in the remaining run's stack, remove the condition relation
                var conditionsToRemove = new HashSet<Activity>(conditions.Except(_run));

                if (conditionsToRemove.Count > 0) // A condition is about to be removed
                {
                    graphAltered = true;
                }
                foreach (var con in conditionsToRemove)
                {
                    Graph.RemoveCondition(con.Id, currentActivity.Id);
                }
            }

            _run.Enqueue(currentActivity);
            _last = currentActivity;

            return graphAltered;
        }

        public bool Stop()
        {
            bool graphAltered = false;

            //set things that have not been run to not pending.
            foreach (var ac in Graph.Activities.Except(_run))
            {
                if (ac.Pending)
                {
                    Graph.SetPending(false, ac.Id);
                    graphAltered = true;
                }
            }

            while (_run.Count > 0)
            {
                var a1 = _run.Dequeue(); //the next element

                HashSet<Activity> responses;

                //if it has no response relations; continue.
                if (!Graph.Responses.TryGetValue(a1, out responses))
                    continue;

                //if an element is in responses and not in the remaining run, remove the element from responses
                var newResponses = new HashSet<Activity>(responses.Intersect(_run));

                if (newResponses.Count != responses.Count) // A response is about to be removed
                {
                    graphAltered = true;
                }

                //we could remove the keyvalue pair, if the resulting set is empty
                //then the elements has no responses
                if (newResponses.Count == 0)
                {
                    Graph.Responses.Remove(a1);
                }
                else
                {
                    Graph.Responses[a1] = newResponses;
                }
            }
            


            _allRuns.Remove(_runId);
            _runId = null;
            _run = new Queue<Activity>();
            _last = null;

            return graphAltered;
        }

        //if it has not seen the 'id' before it will assume that it is a new trace.
        public bool Stop(string id)
        {
            if (id != _runId)
            { // add the currentRun to dictionary, if not the one we want to stop.
                if (_runId != null)
                    _allRuns[_runId] = _run;
                if (_allRuns.TryGetValue(id, out _run))
                { //get the one we want to stop.
                    _runId = id;
                }
                else
                { //new empty run.
                    _run = new Queue<Activity>();
                    _runId = id;
                }
            }
            return Stop();
        }

        //used to add one complete trace.
        public bool AddTrace(LogTrace trace)
        {
            //maybe run stop first-?

            bool graphAltered = false;
            foreach (LogEvent e in trace.Events)
            {
                if (AddEvent(e.IdOfActivity, trace.Id))
                {
                    graphAltered = true;
                }
            }
            var stopAlteredGraph = Stop();
            return graphAltered || stopAlteredGraph;
        }

        public bool AddLog(Log log)
        {
            bool graphAltered = false;
            foreach (var trace in log.Traces)
            {
                if (this.AddTrace(trace))
                {
                    graphAltered = true;
                }
            }
            return graphAltered;
        }

        public void AddActivity(Activity a)
        {
            Graph.AddActivity(a.Id, a.Name);

            foreach (var act in Graph.Activities.Except(new List<Activity> {a}))
            {
                // Excludes
                Graph.AddIncludeExclude(false, a.Id, act.Id);
                Graph.AddIncludeExclude(false, act.Id, a.Id);

                // Responses
                Graph.AddResponse(a.Id, act.Id);
                Graph.AddResponse(act.Id, a.Id);
            }
        }

        public static bool CanMakeNested(DcrGraph graph, HashSet<Activity> activities)
        {
            if (activities.Count() < MinimumNestedSize)
                return false;
            //for each relation to and from the activities (not including eachother), 
            //if they exist for all activities, then return true.
            return forAll(activities, graph.Conditions) && forAll(activities, graph.Responses) &&
                   forAll(activities, graph.Milestones) && ForAllIncOrExcludes(activities, graph.IncludeExcludes) && HasIngoingConnections(graph, activities);
        }

        //Helper method for the CanMakeNested-check, to see if all 'activities' fulfill the same purpose in all relationpairs
        private static bool forAll(HashSet<Activity> activities, IEnumerable<KeyValuePair<Activity,HashSet<Activity>>> relationPairs )
        {
            foreach (var sourceTargetsPair in relationPairs)
            {
                //else if one of the activities is a target, they all have to be.
                //does there exist an activity that is in the targets, but the others are not.
                if (!activities.Any(a => Equals(a, sourceTargetsPair.Key)) &&
                !AllOrNoneInThisSet(activities, sourceTargetsPair.Value))
                {
                    return false;
                }
            }
            return true;
        }


        //if the activities has any relation where they are the target and the source is not in activities
        private static bool HasIngoingConnections(DcrGraph graph, HashSet<Activity> activities) =>
             graph.IncludeExcludes.Any(keyValuePair => activities.All(a => !Equals(a, keyValuePair.Key)) &&
                                                             activities.Any(a => keyValuePair.Value.ContainsKey(a)))
                   || graph.Conditions.Any(keyValuePair => activities.All(a => !Equals(a, keyValuePair.Key)) &&
                                                        activities.Any(a => keyValuePair.Value.Contains(a)))
                   || graph.Responses.Any(keyValuePair => activities.All(a => !Equals(a, keyValuePair.Key)) &&
                                                       activities.Any(a => keyValuePair.Value.Contains(a)))
                   || graph.Milestones.Any(keyValuePair => activities.All(a => !Equals(a, keyValuePair.Key)) &&
                                                           activities.Any(a => keyValuePair.Value.Contains(a)));


        //Helper method for the CanMakeNested-check, to see if all 'activities' fulfill the same purpose in all Include or Exclude relationpairs
        private static bool ForAllIncOrExcludes(HashSet<Activity> activities, IEnumerable<KeyValuePair<Activity, Dictionary<Activity,bool>>> relationPairs)
        {
            foreach (var sourceTargetsPair in relationPairs)
            {
                //if one of the activities is a target, they all have to be.
                //does there exist an activity that is in the targets, but the others are not.
                if (!activities.Any(a => Equals(a, sourceTargetsPair.Key)) &&
                !AllOrNoneInThisSet(activities, sourceTargetsPair.Value))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AllOrNoneInThisSet(HashSet<Activity> activities, HashSet<Activity> set) => 
            activities.All(set.Contains) 
            ||
            !set.Any(activities.Contains);

        private static bool AllOrNoneInThisSet(HashSet<Activity> activities, Dictionary<Activity, bool> dict)
        {
            foreach (var ac in activities)
            {
                bool inOrExOf;
                if (dict.TryGetValue(ac, out inOrExOf))
                {
                    foreach (var ac2 in activities)
                    {
                        bool other;
                        if (dict.TryGetValue(ac2, out other))
                        {
                            if (other != inOrExOf)
                                return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        //With help from stackoverflow
        private static void GetSubsets(List<Activity> superSet, int k, int idx,  HashSet<Activity> current,  List<HashSet<Activity>> solution)
        {
            //When we find a possible permutation
            if (current.Count() == k)
            {
                solution.Add(new HashSet<Activity>(current.Select(a => a.Copy())));
                return;
            }
            if (idx == superSet.Count()) return;
            Activity x = superSet[idx];
            current.Add(x);
            //"guess" x is in the subset
            GetSubsets(superSet, k, idx + 1, current, solution);
            current.Remove(x);
            //"guess" x is not in the subset
            GetSubsets(superSet, k, idx + 1, current, solution);
        }
        
        public static DcrGraph CreateNests(DcrGraph graph)
        {
            var copy = graph.Copy();
            List<HashSet<Activity>> combinations = new List<HashSet<Activity>>();

            //for all tuples of the size activites.count -1 size down to the minimumSize. Try if they can be made nested.
            for (int numberToTry = copy.Activities.Count - 1; numberToTry >= MinimumNestedSize; numberToTry--)
            {
                GetSubsets(copy.Activities.ToList(),numberToTry,0,new HashSet<Activity>(),combinations);
            }

            foreach (var activities in combinations)
            {
                if (CanMakeNested(copy, activities))
                {
                    copy.MakeNestedGraph(activities);
                    //if we actually make a nested graph. Make it and then call the create nest-method again.
                    copy = CreateNests(copy);
                }
            }

            return copy;
        }

        //for conditions & Milestones.
        public static DcrGraph PostProcessing(DcrGraph graph)
        {
            var copy = graph.Copy();

            copy = CreateNests(copy);
            
            PostProcessingResultEvent?.Invoke(copy);
            return copy;
        }
        
        
    }
}
