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

        internal void addActivity(string id, string name)
        {
            throw new NotImplementedException();
        }

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

            
            foreach (var sourcePair in IncludeExcludes)
            {
                var source = sourcePair.Key;
                foreach (var targetPair in sourcePair.Value)
                {
                    var incOrEx = targetPair.Value ? " -->+ " : " -->% ";

                    returnString += source.Id + incOrEx + targetPair.Key.Id + nl;
                }
            }


            returnString += "\n Responce-relations: \n";

            foreach (var sourcePair in Responses)
            {
                var source = sourcePair.Key;
                foreach (var target in sourcePair.Value)
                {
                    returnString += source.Id + " o--> " + target.Id + nl;
                }
            }

            returnString += "\n Condition-relations: \n";

            foreach (var sourcePair in Conditions)
            {
                var source = sourcePair.Key;
                foreach (var target in sourcePair.Value)
                {
                    returnString += source.Id + " -->o " + target.Id + nl;
                }
            }

            returnString += "\n Milestone-relations: \n";

            foreach (var sourcePair in Milestones)
            {
                var source = sourcePair.Key;
                foreach (var target in sourcePair.Value)
                {
                    returnString += source.Id + " --><> " + target.Id + nl;
                }
            }

            return returnString;
        }
        
    
    }
}
