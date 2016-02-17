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
        Dictionary<Activity, Set<Activity>> Responses;
        Map<Activity, Map<Activity, bool>> InclusionsExclusions;
        Map<Activity, Set<Activity>> Conditions;
        Map<Activity, Set<Activity>> Milestones;
        Map<Activity, Map<Activity, TimeSpan>> Deadlines;

private class Activity
        {
            int Id;
            string Name;
            bool Included;
            bool Executed;
            bool Pending;


        }
    }
}
