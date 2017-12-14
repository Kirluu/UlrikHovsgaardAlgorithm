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
    public class ContradictionApproach
    {
        public static event Action<DcrGraph> PostProcessingResultEvent;

        //This is the mined graph. NOT THE ACTUAL RUNNING GRAPH.
        public DcrGraph Graph = new DcrGraph();
        private Stack<Activity> _run = new Stack<Activity>();
        private string _runId;
        private readonly Dictionary<string, Stack<Activity>> _allRuns = new Dictionary<string, Stack<Activity>>();

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
                    Graph.AddIncludeExclude(false, a1.Id, a2.Id);
                    Graph.AddResponse(a1.Id, a2.Id);
                    Graph.AddCondition(a1.Id, a2.Id);
                }
            }
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
                    _run = new Stack<Activity>();
                    _runId = instanceId;
                }
            }

            Activity currentActivity = Graph.GetActivity(id);
            bool graphAltered = false;
            
            if (_run.Count == 0) // First event of trace
            {
                // Update Excluded-state invocations and violation for currentActivity
                graphAltered |= currentActivity.IncrementExcludedViolation();
                foreach (var graphActivity in Graph.Activities)
                {
                    graphAltered |= graphActivity.IncrementExcludedInvocation();
                }
            }
            else 
            {
                var lastActivity = Graph.GetActivity(_last.Id);
                bool firstViolation = true;
                var runArr = _run.ToArray();

                //traversing run in reverse order, cause stack
                for (int i = runArr.Length - 2; i > 0; i--)
                {
                    if (runArr[i + 1].Equals(lastActivity) && runArr[i].Equals(currentActivity))
                        firstViolation = false;
                }

                // Exclude-relation from _last to current has been violated (Counts towards exchanging the exclusion with an inclusion, or if self-excl: removal of excl.)
                if(firstViolation)
                    graphAltered |= Graph.IncludeExcludes[lastActivity][currentActivity].IncrViolations();
            }
            
            bool firstOccurrenceInTrace = !_run.Contains(currentActivity);
            if (firstOccurrenceInTrace)
            {
                // Update invocation-counter for all outgoing Inclusion/Exclusion relations
                foreach (var target in Graph.IncludeExcludes[currentActivity])
                {
                    graphAltered |= target.Value.IncrInvocations();
                }


                var otherActivities = Graph.Activities.Where(x => !x.Equals(currentActivity));
                foreach (var conditionSource in otherActivities)
                {
                    // Register ingoing condition-violation for activities that have not been run before in the current trace
                    var conditionViolated = !_run.Contains(conditionSource);
                    graphAltered |= Graph.Conditions[conditionSource][currentActivity].Increment(conditionViolated);
                }
            }

            _run.Push(currentActivity);
            _last = currentActivity;

            return graphAltered;
        }

        public bool Stop()
        {
            bool graphAltered = false;

            // Increment Pending-states invocations (and mayhaps violations of the constraint) for all activities
            var notInTrace = new HashSet<Activity>(Graph.Activities.Except(_run));
            foreach (var act in Graph.Activities)
            {
                var violationOccurred = notInTrace.Contains(act);

                // Invoke Pending + self-condition statistics for all activities in trace
                graphAltered |= act.IncrementPendingInvocation();
                graphAltered |= Graph.Conditions[act][act].IncrInvocations();

                if (violationOccurred)
                {
                    // Didn't occur in trace --> Pending-belief is violated:
                    graphAltered |= act.IncrementPendingViolation();
                }
                else
                {
                    // Did occur in trace --> Self-cindition is violated: (Registered here to get max 1 violations pr. trace)
                    graphAltered |= Graph.Conditions[act][act].IncrViolations();
                }
            }

            // Evaluate Responses for all activities
            var activitiesConsidered = new HashSet<Activity>();
            while (_run.Count > 0)
            {
                var act = _run.Pop(); //the next element
                if (activitiesConsidered.Contains(act))
                    continue;
                activitiesConsidered.Add(act);

                var otherActivities = Graph.Activities.Where(x => !x.Equals(act));

                // First time we consider "act": All other activities not already considered
                //     have had their Response-relation from "act" violated, as they did not occur later
                foreach (var otherAct in otherActivities)
                {
                    var responseViolated = !activitiesConsidered.Contains(otherAct);
                    graphAltered |= Graph.Responses[act][otherAct].Increment(responseViolated);
                }
            }

            _allRuns.Remove(_runId);
            _runId = null;
            _run = new Stack<Activity>();
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
                    _run = new Stack<Activity>();
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
                graphAltered |= AddEvent(e.IdOfActivity, trace.Id);
            }
            bool stopAlteredGraph = false;
            if (trace.Events.Count > 0)
                stopAlteredGraph = Stop();
            return graphAltered || stopAlteredGraph;
        }

        public bool AddLog(Log log)
        {
            bool graphAltered = false;
            foreach (var trace in log.Traces)
            {
                graphAltered |= this.AddTrace(trace);
            }
            return graphAltered;
        }
        

        public static bool CanMakeNested(DcrGraph graph, HashSet<Activity> activities)
        {
            if (activities.Count() < MinimumNestedSize)
                return false;
            //for each relation to and from the activities (not including eachother), 
            //if they exist for all activities, then return true.
            return forAll(activities, graph.Conditions) && forAll(activities, graph.Responses) &&
                   forAll(activities, graph.Milestones) && forAll(activities, graph.IncludeExcludes) && HasIngoingConnections(graph, activities);
        }

        //Helper method for the CanMakeNested-check, to see if all 'activities' fulfill the same purpose in all relationpairs
        private static bool forAll(HashSet<Activity> activities, IEnumerable<KeyValuePair<Activity,Dictionary<Activity, Confidence>>> relationPairs )
        {
            foreach (var sourceTargetsPair in relationPairs)
            {
                //else if one of the activities is a target, they all have to be.
                //does there exist an activity that is in the targets, but the others are not.
                if (!activities.Any(a => Equals(a, sourceTargetsPair.Key)) &&
                !AllOrNoneInThisSet(activities, new HashSet<Activity>(sourceTargetsPair.Value.Keys)))
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
                                                        activities.Any(a => keyValuePair.Value.Keys.Contains(a)))
                   || graph.Responses.Any(keyValuePair => activities.All(a => !Equals(a, keyValuePair.Key)) &&
                                                       activities.Any(a => keyValuePair.Value.Keys.Contains(a)))
                   || graph.Milestones.Any(keyValuePair => activities.All(a => !Equals(a, keyValuePair.Key)) &&
                                                           activities.Any(a => keyValuePair.Value.Keys.Contains(a)));


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
        
        public static DcrGraph PostProcessing(DcrGraph graph)
        {
            var copy = graph.Copy();

            //copy = CreateNests(copy); // TODO: Implement nested graphs for statistics graphs - use ByteDcrGraph?
            
            PostProcessingResultEvent?.Invoke(copy);
            return copy;
        }
    }
}
