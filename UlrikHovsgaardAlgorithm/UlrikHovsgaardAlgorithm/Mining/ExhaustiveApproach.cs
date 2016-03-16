using System;
using System.Collections.Generic;
using System.Linq;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.GraphSimulation;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithm.Mining
{
    internal class ExhaustiveApproach
    {
        //This is the mined graph. NOT THE ACTUAL RUNNING GRAPH.
        internal DcrGraph Graph = new DcrGraph();
        List<Activity> _run = new List<Activity>();
        //HashSet<Activity> _included;
        Activity _last;

        public ExhaustiveApproach(HashSet<Activity> activities)
        {
            //initialising activities
            foreach (var a in activities)
            {
                Graph.AddActivity(a.Id, a.Name);
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
                    Graph.AddResponse(a1.Id, a2.Id);
                }
            }


            //_included = Graph.GetIncludedActivities();

        }

        internal void AddEvent(string id)
        {
            Activity currentActivity = Graph.GetActivity(id);
            if (_run.Count == 0)
                currentActivity.Included = true;
            else
            {
                //last activity now includes the current activity
                Graph.AddIncludeExclude(true, _last.Id, currentActivity.Id);
            }
            //the acticity has been included at some point : Do we need this if we can just get the included activities in the end?
            //_included.Add(currentActivity);

            _run.Add(currentActivity);
            _last = currentActivity;
        }

        internal void Stop()
        {
            //set things that have not been run to not pending.
            foreach (var ac in Graph.Activities.Except(_run))
            {
                Graph.SetPending(false,ac.Id);
            }
            
            while (_run.Count > 0)
            {
                var a1 = _run.First();
                _run.Remove(a1); //the next element

                HashSet<Activity> responses;

                //if it has no response relations; continue.
                if (!Graph.Responses.TryGetValue(a1, out responses))
                    continue;
                //if an element is in responses and not in the remaining run, remove the element from responses
                var newResponses = new HashSet<Activity>(responses.Intersect(_run));

                //we could remove the keyvalue pair, if the resulting set is empty
                if (newResponses.Count == 0)
                {
                    Graph.Responses.Remove(a1);
                }
                else
                {
                    Graph.Responses[a1] = newResponses;
                }
            }

        _run = new List<Activity>();
            _last = null;
        }

        internal void AddTrace(LogTrace trace)
        {
            //maybe run stop first-?

            foreach (LogEvent e in trace.Events)
            {
                AddEvent(e.Id);
            }
            this.Stop();
        }

        //for conditions
        public void PostProcessing()
        {
            var traceFinder = new UniqueTraceFinderWithComparison(Graph);

            //testing if we an replace any include relations with conditions.
            foreach (var source in Graph.Activities)
            {
                //if it is an include relation and the target activity is excluded
                foreach (var includeTarget in Graph.GetIncludeOrExcludeRelation(source,true))
                {
                    if (!includeTarget.Included)
                    {
                        //remove the relation and set the 
                        var copyGraph = Graph.Copy();
                        copyGraph.SetIncluded(true,includeTarget.Id);
                        copyGraph.RemoveIncludeExclude(source.Id,includeTarget.Id);
                        copyGraph.AddCondition(source.Id,includeTarget.Id);

                        if (traceFinder.CompareTracesFoundWithSupplied(copyGraph))
                        {
                            Graph = copyGraph;
                            Console.WriteLine("Include replaced with condition");
                        }
                    }
                }
            }
        }
    }
}