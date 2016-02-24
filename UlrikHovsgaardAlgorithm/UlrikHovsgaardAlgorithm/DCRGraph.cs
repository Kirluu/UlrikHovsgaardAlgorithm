using System;
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
        public bool Running { get; set; } = false;

        #endregion

        /// <summary>
        /// Enumerates source DcrGraph's activities and looks for differences in states between the source and the target (compared DcrGraph)
        /// </summary>
        /// <param name="comparedDcrGraph">The DcrGraph that the source DcrGraph is being compared to</param>
        /// <returns></returns>
        public bool AreInEqualState(DcrGraph comparedDcrGraph)
        {
            foreach (var activity in Activities)
            {
                // Get corresponding activity
                var comparedActivity = comparedDcrGraph.Activities.Single(a => a.Id == activity.Id);
                // Compare values
                if (activity.Executed != comparedActivity.Executed || activity.Included != comparedActivity.Included ||
                    activity.Pending != comparedActivity.Pending)
                {
                    return false;
                }
            }
            return true;
        }
        
        public DcrGraph Copy()
        {
            var newDcrGraph = new DcrGraph();
            
            // Activities
            newDcrGraph.Activities = CloneHashSet<Activity>(Activities);

            // Responses
            foreach (var response in Responses)
            {
                newDcrGraph.Responses.Add(response.Key, CloneHashSet<Activity>(response.Value));
            }

            // Includes and Excludes
            foreach (var inclusionExclusion in IncludeExcludes)
            {
                var activityBoolCopy = inclusionExclusion.Value.ToDictionary(activityBool => activityBool.Key, activityBool => activityBool.Value);
                newDcrGraph.IncludeExcludes.Add(inclusionExclusion.Key, activityBoolCopy);
            }

            // Conditions
            foreach (var condition in Conditions)
            {
                newDcrGraph.Conditions.Add(condition.Key, CloneHashSet<Activity>(condition.Value));
            }

            // Milestones
            foreach (var milestone in Milestones)
            {
                newDcrGraph.Milestones.Add(milestone.Key, CloneHashSet<Activity>(milestone.Value));
            }

            // Deadlines
            foreach (var deadline in Deadlines)
            {
                var deadlineCopy = deadline.Value.ToDictionary(dl => dl.Key, dl => dl.Value);
                newDcrGraph.Deadlines.Add(deadline.Key, deadlineCopy);
            }

            return newDcrGraph;
        }

        public DcrGraph Copy2()
        {
            var newDcrGraph = new DcrGraph();

            // Activities
            foreach (var activity in Activities)
            {
                newDcrGraph.Activities.Add(CopyActivity(activity));
            }

            // Responses
            foreach (var relation in Responses)
            {
                var source = relation.Key;
                foreach (var target in relation.Value)
                {
                    HashSet<Activity> targets;
                    if (newDcrGraph.Responses.TryGetValue(source, out targets))
                    {
                        targets.Add(CopyActivity(target));
                    }
                    else
                    {
                        newDcrGraph.Responses.Add(CopyActivity(source), new HashSet<Activity> { CopyActivity(target) });
                    }
                }
            }

            // Includes and Excludes
            foreach (var relation in IncludeExcludes)
            {
                var source = relation.Key;
                foreach (var keyValuePair in relation.Value)
                {
                    var target = keyValuePair.Key;
                    var incOrEx = keyValuePair.Value;
                    Dictionary<Activity, bool> targets;
                    if (newDcrGraph.IncludeExcludes.TryGetValue(source, out targets))
                    {
                        targets.Add(CopyActivity(target), incOrEx);
                    }
                    else
                    {
                        newDcrGraph.IncludeExcludes.Add(CopyActivity(source), new Dictionary<Activity, bool> { { CopyActivity(target), incOrEx } });
                    }
                }
            }

            // Conditions
            foreach (var relation in Conditions)
            {
                var source = relation.Key;
                foreach (var target in relation.Value)
                {
                    HashSet<Activity> targets;
                    if (newDcrGraph.Conditions.TryGetValue(source, out targets))
                    {
                        targets.Add(CopyActivity(target));
                    }
                    else
                    {
                        newDcrGraph.Conditions.Add(CopyActivity(source), new HashSet<Activity> { CopyActivity(target) });
                    }
                }
            }

            // Milestones
            foreach (var relation in Milestones)
            {
                var source = relation.Key;
                foreach (var target in relation.Value)
                {
                    HashSet<Activity> targets;
                    if (newDcrGraph.Milestones.TryGetValue(source, out targets))
                    {
                        targets.Add(CopyActivity(target));
                    }
                    else
                    {
                        newDcrGraph.Milestones.Add(CopyActivity(source), new HashSet<Activity> { CopyActivity(target) });
                    }
                }
            }

            // Deadlines
            foreach (var relation in Deadlines)
            {
                var source = relation.Key;
                foreach (var keyValuePair in relation.Value)
                {
                    var target = keyValuePair.Key;
                    var timeSpan = keyValuePair.Value;
                    Dictionary<Activity, TimeSpan> targets;
                    if (newDcrGraph.Deadlines.TryGetValue(source, out targets))
                    {
                        targets.Add(CopyActivity(target), timeSpan);
                    }
                    else
                    {
                        newDcrGraph.Deadlines.Add(CopyActivity(source), new Dictionary<Activity, TimeSpan> { { CopyActivity(target), timeSpan } });
                    }
                }
            }

            return newDcrGraph;
        }

        private Activity CopyActivity(Activity input)
        {
            return new Activity {Id = input.Id, Name = input.Name, Roles = input.Roles, Executed = input.Executed, Included = input.Included, Pending = input.Pending};
        }

        

        private HashSet<T> CloneHashSet<T>(HashSet<T> input)
        {
            var array = new T[input.Count];
            input.CopyTo(array);
            return new HashSet<T>(array);
        } 

        private Dictionary<TKey, TValue> CloneDictionaryCloningValues<TKey, TValue>
   (Dictionary<TKey, TValue> original) where TValue : ICloneable
        {
            Dictionary<TKey, TValue> ret = new Dictionary<TKey, TValue>(original.Count,
                                                                    original.Comparer);
            foreach (KeyValuePair<TKey, TValue> entry in original)
            {
                ret.Add(entry.Key, (TValue)entry.Value.Clone());
            }
            return ret;
        }

        #region GraphBuilding

        internal void AddActivity(string id, string name)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activities.Add(new Activity
            {
                Id = id,
                Name = name
            });
        }


        internal void AddIncludeExclude(bool incOrEx, string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity, bool> targets;

            if (IncludeExcludes.TryGetValue(fstActivity, out targets)) // then last already has relations
            {
                if (firstId == secondId && incOrEx)
                {
                    //if we try to add an include to the same activity, just delete the old possible exclude
                    if (targets.ContainsKey(fstActivity))
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
                    targets = new Dictionary<Activity, bool> { { sndActivity, incOrEx } };
                    IncludeExcludes[fstActivity] = targets;
                }
            }
        }

        //addresponce Condition and milestone should probably be one AddRelation method, that takes an enum.
        internal void AddResponse(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

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
                Responses.Add(fstActivity, new HashSet<Activity>() { sndActivity });
            }

        }

        internal void AddCondition(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            if (firstId == secondId) //because Milestone to one self is not healthy.
                return;

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            HashSet<Activity> targets;

            if (Milestones.TryGetValue(fstActivity, out targets))
            {
                targets.Add(sndActivity);
            }
            else
            {
                Milestones.Add(fstActivity, new HashSet<Activity>() { sndActivity });
            }

        }

        internal void AddMileStone(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            if (firstId == secondId) //because Milestone to one self is not healthy.
                return;

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            HashSet<Activity> targets;

            if (Milestones.TryGetValue(fstActivity, out targets))
            {
                targets.Add(sndActivity);
            }
            else
            {
                Milestones.Add(fstActivity, new HashSet<Activity>() { sndActivity });
            }
                
        }

        #endregion

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


        public HashSet<Activity> GetIncludedActivities()
        {
            return new HashSet<Activity>(Activities.Where(a => a.Included));
        } 

        public bool Execute(Activity a)
        {
            if(!Running)
                throw new InvalidOperationException("It is not permitted to execute an Activity on a Graph, that is not Running.");

            //if the activity is not runnable
            if (!GetRunnableActivities().Contains(a))
                return false; // TODO: Make method void and throw exception here

            var act = GetActivity(a.Id); // TODO: Why? You are given "a", why retrieve "act"?

            //the activity is now executed
            act.Executed = true;

            //it is not pending
            act.Pending = false;

            //its responce relations are now pending.
            HashSet<Activity> respTargets;
            if (Responses.TryGetValue(act, out respTargets))
            {
                foreach (Activity respActivity in respTargets)
                {
                    respActivity.Pending = true;
                }
            }

            //its include/exclude relations are now included/excluded.
            Dictionary<Activity, bool> incExcTargets;
            if (IncludeExcludes.TryGetValue(act, out incExcTargets))
            {
                foreach (var keyValuePair in incExcTargets)
                {
                    keyValuePair.Key.Included = keyValuePair.Value;
                }
            }

            return true;
        }

        public HashSet<Activity> GetRunnableActivities()
        {
            //if the activity is included.
            var included = GetIncludedActivities();

            var conditionTargets = new HashSet<Activity>();
            foreach (var source in included)
            {
                var targets = new HashSet<Activity>();
                //and no other included and non-executed activity has a condition to it
                if (!source.Executed && Conditions.TryGetValue(source, out targets))
                {
                    conditionTargets.UnionWith(targets);
                }

                //and no other included and pending activity has a milestone relation to it.
                if (source.Pending && Milestones.TryGetValue(source, out targets))
                {
                    conditionTargets.UnionWith(targets);
                }

            }

            included.ExceptWith(conditionTargets);

            return included;
        }

        public bool IsFinalState()
        {
            return !Activities.Any(a => a.Included && a.Pending);
        }

        public override string ToString()
        {
            var returnString = "Activities: \n";
            const string nl = "\n";

            foreach (var a in Activities)
            {
                returnString += a.Id + " : " + a.Name + " inc=" + a.Included + ", pnd=" + a.Pending + ", exe=" + a.Executed + nl;
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
