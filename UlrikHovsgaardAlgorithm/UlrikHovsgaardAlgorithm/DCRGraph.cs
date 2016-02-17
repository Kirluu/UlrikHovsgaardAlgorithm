using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class DCRGraph
    {
        HashSet<Activity> Activities;
        Dictionary<Activity, HashSet<Activity>> Responses;
        Dictionary<Activity, Dictionary<Activity, bool>> IncludeExcludes; // bool TRUE is include
        Dictionary<Activity, HashSet<Activity>> Conditions;
        Dictionary<Activity, HashSet<Activity>> Milestones;
        Dictionary<Activity, Dictionary<Activity, TimeSpan>> Deadlines;


        public override string ToString()
        {
            var returnString = "Activities: \n";
            var nl = "\n";

            foreach (var a in Activities)
            {
                returnString += a.Id + " : " + a.Name + nl;
            }

            returnString += "\n Include-/exclude-relations: \n";

            foreach (var source in Activities)
            {
                if (IncludeExcludes.ContainsKey(source))
                    foreach (var targetPair in IncludeExcludes[source])
                    {
                        var incOrEx = targetPair.Value ? " ->+ " : " ->% ";

                        returnString += source.Id + incOrEx + targetPair.Key.Id + nl;
                    }
            }


            returnString += "\n Responce-relations: \n";

            foreach (var source in Activities)
            {
                if (Responses.ContainsKey(source))
                    foreach (var target in Responses[source])
                    {
                        returnString += source.Id + " o-> " + target.Id + nl;
                    }
            }

            returnString += "\n Condition-relations: \n";

            foreach (var source in Activities)
            {
                if (Conditions.ContainsKey(source))
                    foreach (var target in Conditions[source])
                    {
                        returnString += source.Id + " ->o " + target.Id + nl;
                    }
            }

            returnString += "\n Milestone-relations: \n";

            foreach (var source in Activities)
            {
                if (Milestones.ContainsKey(source))
                    foreach (var target in Milestones[source])
                    {
                        returnString += source.Id + " -><> " + target.Id + nl;
                    }
            }

            return returnString;
        }

    private class Activity
            {
                public int Id;
                public string Name;
                bool Included;
                bool Executed;
                bool Pending;
            }

        private class Process
        {
            HashSet<Event> Alphabet;
            List<Trace> Traces;
        }
        private class Event {
            int Id;
        }
        private class Trace {
            int Id;
            List<Event> run;
        }
    
    }
}
