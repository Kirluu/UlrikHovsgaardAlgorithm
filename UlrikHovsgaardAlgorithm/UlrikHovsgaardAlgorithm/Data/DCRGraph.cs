using System;
using System.Collections.Generic;
using System.Linq;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class DcrGraph
    {
        #region Properties

        public HashSet<Activity> Activities { get; set; } = new HashSet<Activity>();
        public Dictionary<Activity, HashSet<Activity>> Responses { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, Dictionary<Activity, bool>> IncludeExcludes { get; } = new Dictionary<Activity, Dictionary<Activity, bool>>(); // bool TRUE is include
        public Dictionary<Activity, HashSet<Activity>> Conditions { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Milestones { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, Dictionary<Activity, TimeSpan>> Deadlines { get; } = new Dictionary<Activity, Dictionary<Activity, TimeSpan>>();
        public bool Running { get; set; } = false;

        #endregion

        public Activity GetActivity(string id)
        {
            return Activities.Single(a => a.Id == id);
        }

        #region GraphBuilding methods

        //TODO: Make addRelation method that takes an enum, instead of five different methods.
        public void AddActivity(string id, string name)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activities.Add(new Activity(id, name));
        }

        public void SetPending(bool pending, string id)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            GetActivity(id).Pending = pending;
        }

        public void SetIncluded(bool included, string id)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            GetActivity(id).Included = included;
        }

        public void AddIncludeExclude(bool incOrEx, string firstId, string secondId)
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
        public void AddResponse(string firstId, string secondId)
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

        public void AddCondition(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            if (firstId == secondId) //because Condition to one self is not healthy.
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

        public void AddMileStone(string firstId, string secondId)
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

        public void RemoveIncludeExclude(string firstId, string secondId)
        {

            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity, bool> targets;

            if (IncludeExcludes.TryGetValue(fstActivity, out targets))
            {
                if (targets.ContainsKey(sndActivity))
                {
                    targets.Remove(sndActivity);
                }
            }
        }

        #endregion

        #region Run-related methods

        public HashSet<Activity> GetRunnableActivities()
        {
            //if the activity is included.
            var included = GetIncludedActivities();

            var conditionTargets = new HashSet<Activity>();
            foreach (var source in included)
            {
                HashSet<Activity> targets;
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

        public bool Execute(Activity a)
        {
            if(!Running)
                throw new InvalidOperationException("It is not permitted to execute an Activity on a Graph, that is not Running.");

            //if the activity is not runnable
            if (!GetRunnableActivities().Contains(a))
                return false; // TODO: Make method void and throw exception here

            var act = GetActivity(a.Id); // TODO: Why? You are given "a", why retrieve "act"?
            //var act = a;

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
                    GetActivity(respActivity.Id).Pending = true;
                    //respActivity.Pending = true;
                }
            }

            //its include/exclude relations are now included/excluded.
            Dictionary<Activity, bool> incExcTargets;
            if (IncludeExcludes.TryGetValue(act, out incExcTargets)) 
            {
                foreach (var keyValuePair in incExcTargets)
                {
                    GetActivity(keyValuePair.Key.Id).Included = keyValuePair.Value;
                    //keyValuePair.Key.Included = keyValuePair.Value;
                }
            }

            return true;
        }

        public bool IsFinalState()
        {
            return !Activities.Any(a => a.Included && a.Pending);
        }

        public HashSet<Activity> GetIncludedActivities()
        {
            return new HashSet<Activity>(Activities.Where(a => a.Included));
        }

        #endregion

        #region Utilitary methods (IsEqualState, Copy, ExportToXml, ToString)

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
            newDcrGraph.Activities = CloneActivityHashSet(Activities);

            // Responses
            foreach (var response in Responses)
            {
                newDcrGraph.Responses.Add(CopyActivity(response.Key), CloneActivityHashSet(response.Value));
            }

            // Includes and Excludes
            foreach (var inclusionExclusion in IncludeExcludes)
            {
                var activityBoolCopy = inclusionExclusion.Value.ToDictionary(activityBool => CopyActivity(activityBool.Key), activityBool => activityBool.Value);
                newDcrGraph.IncludeExcludes.Add(CopyActivity(inclusionExclusion.Key), activityBoolCopy);
            }

            // Conditions
            foreach (var condition in Conditions)
            {
                newDcrGraph.Conditions.Add(CopyActivity(condition.Key), CloneActivityHashSet(condition.Value));
            }

            // Milestones
            foreach (var milestone in Milestones)
            {
                newDcrGraph.Milestones.Add(CopyActivity(milestone.Key), CloneActivityHashSet(milestone.Value));
            }

            // Deadlines
            foreach (var deadline in Deadlines)
            {
                var deadlineCopy = deadline.Value.ToDictionary(dl => CopyActivity(dl.Key), dl => dl.Value);
                newDcrGraph.Deadlines.Add(CopyActivity(deadline.Key), deadlineCopy);
            }

            return newDcrGraph;
        }

        private Activity CopyActivity(Activity input)
        {
            return new Activity(input.Id, input.Name) { Roles = input.Roles, Executed = input.Executed, Included = input.Included, Pending = input.Pending };
        }

        private HashSet<Activity> CloneActivityHashSet(HashSet<Activity> source)
        {
            var result = new HashSet<Activity>();
            foreach (var activity in source)
            {
                result.Add(CopyActivity(activity));
            }
            return result;
        }

        public HashSet<Activity> GetIncludeOrExcludeRelation(Activity source, bool incl)
        {
            Dictionary<Activity,bool> dict;
            if (IncludeExcludes.TryGetValue(source, out dict))
            {
            HashSet<Activity> set = new HashSet<Activity>();

                foreach (var target in dict)
            {
                if (target.Value == incl)
                    set.Add(target.Key);
            }

            return set;
            }
            else
            {
                return new HashSet<Activity>();
            }

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

        public string ExportToXml()
        {
            var xml = "<dcrgraph>\n";

            xml += "<specification>\n<resources>\n<events>\n"; // Begin events
            // Event definitions
            foreach (var activity in Activities)
            {
                xml += string.Format(@"<event id=""{0}"" scope=""private"" >
    <custom>
        <visualization>
            <location xLoc = ""806"" yLoc=""183"" />
        </visualization>
        <roles>
            <role></role>
        </roles>
        <groups>
            <group />
        </groups>
        <eventType></eventType>
        <eventDescription></eventDescription>
        <level>1</level>
        <eventData></eventData>
    </custom>
</event>", activity.Id); // Consider removing location ? What will happen?
                xml += "\n";
            }

            xml += "</events>\n"; // End events
            xml += "<subProcesses></subProcesses>\n";
            xml += @"<distribution>
    <externalEvents></externalEvents>
</distribution>";
            xml += "\n";
            // Labels
            xml += "<labels>\n";
            foreach (var activity in Activities)
            {
                xml += string.Format(@"<label id =""{0}""/>", activity.Name);
                xml += "\n";
            }
            xml += "</labels>\n";
            // Label mappings
            xml += "<labelMappings>\n";
            foreach (var activity in Activities)
            {
                xml += string.Format(@"<labelMapping eventId =""{0}"" labelId = ""{1}""/>", activity.Id, activity.Name);
                xml += "\n";
            }
            xml += "</labelMappings>\n";
            // Stuff
            xml += @"<expressions></expressions>
    <variables></variables>
    <variableAccesses>
        <writeAccesses />
    </variableAccesses>
    <custom>
        <roles></roles>
        <groups></groups>
        <eventTypes></eventTypes>
        <graphDetails></graphDetails>
        <graphFilters>
            <filteredGroups></filteredGroups>
            <filteredRoles></filteredRoles>
        </graphFilters>
    </custom>
</resources>";
            xml += "\n";

            // Constraints
            xml += "<constraints>\n";
            // Conditions
            xml += "<conditions>\n";
            foreach (var condition in Conditions)
            {
                foreach (var target in condition.Value)
                {
                    xml += string.Format(@"<exclude sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", condition.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</conditions>\n";

            // Responses
            xml += "<responses>\n";
            foreach (var response in Responses)
            {
                foreach (var target in response.Value)
                {
                    xml += string.Format(@"<response sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", response.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</responses>\n";

            // Excludes
            xml += "<excludes>\n";
            foreach (var exclusion in IncludeExcludes)
            {
                foreach (var target in exclusion.Value)
                {
                    if (!target.Value) // If it is an exclusion
                    {
                        xml += string.Format(@"<exclude sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", exclusion.Key.Id, target.Key.Id);
                        xml += "\n";
                    }
                }
            }
            xml += "</excludes>\n";

            // Includes
            xml += "<includes>\n";
            foreach (var inclusion in IncludeExcludes)
            {
                foreach (var target in inclusion.Value)
                {
                    if (target.Value) // If it is an inclusion
                    {
                        xml += string.Format(@"<include sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", inclusion.Key.Id, target.Key.Id);
                        xml += "\n";
                    }
                }
            }
            xml += "</includes>\n";

            // Milestones
            xml += "<milestones>\n";
            foreach (var milestone in Milestones)
            {
                foreach (var target in milestone.Value)
                {
                    xml += string.Format(@"<milestone sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", milestone.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</milestones>\n";
            // Spawns
            xml += "<spawns></spawns>\n";
            xml += "</constraints>\n";
            xml += "</specification>\n";
            // End constraints

            // Start states
            xml += @"<runtime>
<marking>
    <globalStore></globalStore>";
            xml += "\n";
            // Executed events
            xml += "<executed>\n";
            foreach (var activity in Activities)
            {
                if (activity.Executed)
                {
                    xml += string.Format(@"<event id=""{0}""/>", activity.Id);
                    xml += "\n";
                }
            }
            xml += "</executed>\n";
            // Incuded events
            xml += "<included>\n";
            foreach (var activity in Activities)
            {
                if (activity.Included)
                {
                    xml += string.Format(@"<event id=""{0}""/>", activity.Id);
                    xml += "\n";
                }
            }
            xml += "</included>\n";
            // Pending events
            xml += "<pendingResponses>\n";
            foreach (var activity in Activities)
            {
                if (activity.Pending)
                {
                    xml += string.Format(@"<event id=""{0}""/>", activity.Id);
                    xml += "\n";
                }
            }
            xml += "</pendingResponses>\n";
            xml += @"</marking>
    <custom />
</runtime>";
            // End start states

            // End DCR Graph
            xml += "\n</dcrgraph>";
            return xml;
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

        #endregion
    }
}
