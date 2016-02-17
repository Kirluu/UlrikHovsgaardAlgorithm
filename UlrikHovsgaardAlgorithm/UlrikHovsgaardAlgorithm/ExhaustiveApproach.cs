using System;
using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm
{
    internal class ExhaustiveApproach
    {
        internal DcrGraph Graph = new DcrGraph();
        List<Activity> _run = new List<Activity>();
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


        }

        internal void AddEvent(string id)
        {
            Activity currentActivity = Graph.GetActivity(id);
            if (_run.Count == 0)
                currentActivity.Included = true;
            else if (currentActivity.Included == false)
            {
                //last activity now includes the current activity
                Graph.AddIncludeExclude(true, _last, currentActivity);
            }
            

            _run.Add(currentActivity);
            _last = currentActivity;
        }

        internal void Stop()
        {
            //set things that have not been run to not pending.
        }
    }
}