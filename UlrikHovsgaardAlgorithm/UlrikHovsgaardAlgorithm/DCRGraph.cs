﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class DcrGraph
    {
        #region Properties

        public HashSet<Activity> Activities { get; set; } = new HashSet<Activity>();
        public Dictionary<Activity, HashSet<Activity>> Responses { get; set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, Dictionary<Activity, bool>> IncludeExcludes { get; set; } = new Dictionary<Activity, Dictionary<Activity, bool>>(); // bool TRUE is include
        public Dictionary<Activity, HashSet<Activity>> Conditions { get; set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Milestones { get; set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, Dictionary<Activity, TimeSpan>> Deadlines { get; set; } = new Dictionary<Activity, Dictionary<Activity, TimeSpan>>();

        #endregion

        internal void AddActivity(string id, string name)
        {
            Activities.Add(new Activity
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
            return Activities.Single(a => a.Id == id);
        }

        internal void AddIncludeExclude(bool incOrEx, string firstId, string secondId)
        {
            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity, bool> targets;

            if (IncludeExcludes.TryGetValue(fstActivity, out targets)) // then last already has relations
            {
                if (firstId == secondId && incOrEx)
                {
                    //if we try to add an include to the same activity, just delete the old possible exclude
                    if (targets.ContainsKey(fstActivity))
                    {
                        targets.Remove(sndActivity);
                    }
                }
                else
                    targets[sndActivity] = incOrEx;
            }
            else
            {
                if (!(firstId == secondId && incOrEx))
                    //if we try to add an include to the same activity, just don't
                {
                    targets = new Dictionary<Activity, bool> {{sndActivity, incOrEx}};
                    IncludeExcludes[fstActivity] = targets;
                }
            }
        }

        internal void AddResponse(string firstId, string secondId)
        {
            if (firstId == secondId) //because responce to one self is not healthy.
                return;

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            HashSet<Activity> targets;

            if (Responses.TryGetValue(fstActivity, out targets))
            {
                targets.Add(sndActivity);
            }
            else
            {
                Responses.Add(fstActivity, new HashSet<Activity>() {sndActivity});
            }
                
        }

        public HashSet<Activity> GetIncludedActivities()
        {
            return Activities.Select(a => a.Included) as HashSet<Activity>;
        } 

        public override string ToString()
        {
            var returnString = "Activities: \n";
            const string nl = "\n";

            foreach (var a in Activities)
            {
                returnString += a.Id + " : " + a.Name +" inc=" +a.Included + ", pnd=" + a.Pending + ", exe=" + a.Executed + nl;
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

        public bool Execute(Activity a)
        {
            //if the activity is not runnable
            if (GetRunnableActivities().Select(s => s.Id == a.Id) != null)
                return false;
            throw new NotImplementedException();


            return false;
        }

        public HashSet<Activity> GetRunnableActivities()
        {
            throw new NotImplementedException();

            //if the activity is included.
            var included = this.GetIncludedActivities();

            //and no other included and non-executed activity has a condition to it


            //and no other included and pending activity has a milestone relation to it.



            var acts = new HashSet<Activity>();

            return acts;
        }

        public bool IsStoppable()
        {
            throw new NotImplementedException();

            return false;
        }
        
    
    }
}
