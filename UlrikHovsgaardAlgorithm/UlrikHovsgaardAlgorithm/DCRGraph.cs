using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class DcrGraph
    {
        HashSet<Activity> _activities = new HashSet<Activity>();
        Dictionary<Activity, HashSet<Activity>> _responses = new Dictionary<Activity, HashSet<Activity>>();
        Dictionary<Activity, Dictionary<Activity, bool>> _includeExcludes = new Dictionary<Activity, Dictionary<Activity, bool>>(); // bool TRUE is include
        Dictionary<Activity, HashSet<Activity>> _conditions = new Dictionary<Activity, HashSet<Activity>>();
        Dictionary<Activity, HashSet<Activity>> _milestones = new Dictionary<Activity, HashSet<Activity>>();
        Dictionary<Activity, Dictionary<Activity, TimeSpan>> _deadlines = new Dictionary<Activity, Dictionary<Activity, TimeSpan>>();


        internal void AddActivity(string id, string name)
        {
            _activities.Add(new Activity
            {
                Id = id,
                Name = name
            });
        }

        internal void SetPending(bool pending, string id)
        {
            GetActivity(id).Pending = pending;
        }

        internal void SetIncluded(bool included, string id)
        {
            GetActivity(id).Included = included;
        }

        internal Activity GetActivity(string id)
        {
            return _activities.Single(a => a.Id == id);
        }

        internal void AddIncludeExclude(bool incOrEx, Activity last, Activity act)
        {
            Dictionary<Activity, bool> targets;

            if (_includeExcludes.TryGetValue(last, out targets)) // then last already has relations
            {
                targets.Add(act, incOrEx);
            }
            else
            {
                targets = new Dictionary<Activity, bool> {{act, incOrEx}};
                _includeExcludes.Add(last,targets);
            }
        }


        public override string ToString()
        {
            var returnString = "Activities: \n";
            const string nl = "\n";

            foreach (var a in _activities)
            {
                returnString += a.Id + " : " + a.Name + nl;
            }

            returnString += "\n Include-/exclude-relations: \n";

            
            foreach (var sourcePair in _includeExcludes)
            {
                var source = sourcePair.Key;
                foreach (var targetPair in sourcePair.Value)
                {
                    var incOrEx = targetPair.Value ? " -->+ " : " -->% ";

                    returnString += source.Id + incOrEx + targetPair.Key.Id + nl;
                }
            }


            returnString += "\n Responce-relations: \n";

            foreach (var sourcePair in _responses)
            {
                var source = sourcePair.Key;
                foreach (var target in sourcePair.Value)
                {
                    returnString += source.Id + " o--> " + target.Id + nl;
                }
            }

            returnString += "\n Condition-relations: \n";

            foreach (var sourcePair in _conditions)
            {
                var source = sourcePair.Key;
                foreach (var target in sourcePair.Value)
                {
                    returnString += source.Id + " -->o " + target.Id + nl;
                }
            }

            returnString += "\n Milestone-relations: \n";

            foreach (var sourcePair in _milestones)
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
