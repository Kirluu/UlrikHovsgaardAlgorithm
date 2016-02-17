using System;
using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm
{
    internal class ExhaustiveApproach
    {
        internal DCRGraph Graph;

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

        internal void AddEvent(string v)
        {
            throw new NotImplementedException();
        }
    }
}