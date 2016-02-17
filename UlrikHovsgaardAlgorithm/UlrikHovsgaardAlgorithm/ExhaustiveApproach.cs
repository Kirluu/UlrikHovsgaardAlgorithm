using System;
using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm
{
    internal class ExhaustiveApproach
    {
        internal DCRGraph Graph;
        List<Activity> _run;
        Activity _last;

        public ExhaustiveApproach(HashSet<LogEvent> activities)
        {
            

            //initialising activities
            foreach (var a in activities)
            {
                Graph.addActivity(a.Id, a.Name);
                //a is excluded
                Graph.setIncluded(false, a.Id);
                //a is Pending
                Graph.setPending(true, a.Id);
            }


        }

        internal void AddEvent(string id)
        {
            Activity currentActivity = Graph.getActivity(id);
            if (_run.Count == 0)
                currentActivity.Included = true;
            else if (currentActivity.Included == false)
            {
                //last activity now includes the current activity
                Graph.addIncludeExclude(true, _last, currentActivity);
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