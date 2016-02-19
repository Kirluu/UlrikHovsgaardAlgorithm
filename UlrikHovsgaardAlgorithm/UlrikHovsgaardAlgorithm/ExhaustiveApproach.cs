using System;
using System.Collections.Generic;
using System.Linq;

namespace UlrikHovsgaardAlgorithm
{
    internal class ExhaustiveApproach
    {
        internal DcrGraph Graph = new DcrGraph();
        List<Activity> _run = new List<Activity>();
        HashSet<Activity> _included;
        Activity _last;

        public ExhaustiveApproach(HashSet<LogEvent> activities)
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
            //the acticity has been included at some point :
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
            /**
            foreach (var a1 in _run)
            {
                _run.Remove(a1); //the next element
                
                //for intersectionen mellem included og a1's responces, hvis de ikke findes i den nye run, så slet responcen.
                foreach (var a2 in _included.IntersectWith(Graph.Responses.Single(a => a.Key.Id == a1.Id)))
                {

                }
            }*/

        _run = new List<Activity>();
            _last = null;
        }
    }
}