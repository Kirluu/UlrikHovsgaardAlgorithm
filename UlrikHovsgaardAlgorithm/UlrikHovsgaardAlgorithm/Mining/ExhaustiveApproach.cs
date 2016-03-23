using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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
        private const int MinimumNestedSize = 4;

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
                AddEvent(e.IdOfActivity);
            }
            this.Stop();
        }

        internal void AddLog(Log log)
        {
            foreach (var trace in log.Traces)
            {
                this.AddTrace(trace);
            }
        }

        private bool CanMakeNested(params Activity[] activities)
        {
            if (activities.Count() < MinimumNestedSize)
                return false;
            //TODO:move to method and check for all. TODO: there should be some amount of incoming relations.
            //for each relation to and from the activities (not including eachother), 
            //if they exist for all activities, then return true.
            

            foreach (var sourceTargetsPair in Graph.Conditions)
            {
                

                //if the source is one of the examined activities.
                if (activities.Contains(sourceTargetsPair.Key))
                {
                    //then it has to contain all other of the relations of the other activities
                    if (
                        !sourceTargetsPair.Value.All(
                            a =>
                                activities.All(
                                    (y =>
                                        ((HashSet<Activity>) Graph.Conditions.Select(z => z.Key.Id == y.Id)).Contains(a)))))
                        return false;
                } //else if one of the activities is a target, they all have to be.
                else
                {
                    //does there exist an activity that is in the targets, but the others are not.
                    if (!AllOrNoneInThisSet(activities,sourceTargetsPair.Value))
                    {
                        return false;
                    }
                    
                }

            }
            return true;
        }

        private bool AllOrNoneInThisSet(Activity[] activities, HashSet<Activity> set) => 
            activities.All(set.Contains) 
            ||
            !set.Any(activities.Contains);

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

                        if (traceFinder.CompareTracesFoundWithSuppliedThreaded(copyGraph))
                        {
                            Graph = copyGraph;
                            Console.WriteLine("Include replaced with condition");
                        }
                    }
                }
                HashSet<Activity> conditions;
                if (Graph.Conditions.TryGetValue(source, out conditions))
                { 
                //if it has a Condition relation.
                    foreach (var conditionTarget in conditions)
                    {
                            //remove the relation and set the 
                            var copyGraph = Graph.Copy();
                            copyGraph.RemoveCondition(source.Id, conditionTarget.Id);
                            
                            copyGraph.AddMileStone(source.Id, conditionTarget.Id);

                            if (traceFinder.CompareTracesFoundWithSuppliedThreaded(copyGraph))
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
