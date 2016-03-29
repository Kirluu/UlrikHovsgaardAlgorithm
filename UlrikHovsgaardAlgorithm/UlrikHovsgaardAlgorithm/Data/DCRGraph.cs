using System;
using System.Collections.Generic;
using System.Linq;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class DcrGraph
    {

        #region Properties

        public string Title { get; set; }
        public HashSet<Activity> Activities { get; set; } = new HashSet<Activity>();
        public HashSet<Activity> NestedGraphActivities { get; set; } = new HashSet<Activity>(); 
        public Dictionary<Activity, HashSet<Activity>> Responses { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, Dictionary<Activity, bool>> IncludeExcludes { get; } = new Dictionary<Activity, Dictionary<Activity, bool>>(); // bool TRUE is include
        public Dictionary<Activity, HashSet<Activity>> Conditions { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Milestones { get; } = new Dictionary<Activity, HashSet<Activity>>();

        public bool Running { get; set; }

        #endregion

        public Activity GetActivity(string id)
        {
            return Activities.SingleOrDefault(a => a.Id == id);
        }

        #region GraphBuilding methods

        //TODO: Make addRelation method that takes an enum, instead of five different methods.
        public void AddActivity(string id, string name)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activities.Add(new Activity(id, name));
        }

        public void AddRolesToActivity(string id, List<string> roles)
        {
            GetActivity(id).Roles = roles;
        }

        //Is used to remove the activity and then change the target of other relations to the Nested graph instead.
        public void RemoveActivityFromOuterGraph(string id, Activity nest)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activity act = GetActivity(id);

            Activities.RemoveWhere(a => a.Id == id);

            foreach (var sourceTargetPair in Responses)
            {
                if (sourceTargetPair.Value.Contains(act))
                {
                    sourceTargetPair.Value.RemoveWhere(a => a.Id == id);
                    sourceTargetPair.Value.Add(nest);
                }

            }
            RemoveFromRelation(Conditions,act);
            RemoveFromRelation(Milestones,act);
            RemoveFromRelation(IncludeExcludes,act);
        }

        public void RemoveActivity(string id)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activity act = GetActivity(id);

            Activities.RemoveWhere(a => a.Id == id);

            RemoveFromRelation(Responses, act);
            RemoveFromRelation(Conditions, act);
            RemoveFromRelation(Milestones, act);
            RemoveFromRelation(IncludeExcludes, act);

            act = null;
        }

        //Used when removing activities that should only be in the outer graph (or possibly another Nested graph)
        //The main difference between this method and Remove Activity is that in only removes the activity as source in the relation maps and not as targets
        public void RemoveActivityFromNest(string id)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activity act = GetActivity(id);

            Activities.RemoveWhere(a => a.Id == id);

            Responses.Remove(act);
            Conditions.Remove(act);
            Milestones.Remove(act);
            IncludeExcludes.Remove(act);
        }

        private static void RemoveFromRelation(Dictionary<Activity, HashSet<Activity>> relation, Activity act)
        {
            foreach (var source in relation)
            {
                source.Value.RemoveWhere(a => a.Equals(act));
            }
            relation.Remove(act);
        }

        private static void RemoveFromRelation(Dictionary<Activity, Dictionary<Activity, bool>> incExRelation, Activity act)
        {
            foreach (var source in incExRelation)
            {
                source.Value.Remove(act);
            }
            incExRelation.Remove(act);
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

        public void SetExecuted(bool executed, string id)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            GetActivity(id).Executed = executed;
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
                else // Self-exclude OR include/exclude to other activity than the activity itself
                {
                    targets[sndActivity] = incOrEx;
            }
            }
            else if (!(firstId == secondId && incOrEx)) //if we try to add an include to the same activity, just don't
                {
                    targets = new Dictionary<Activity, bool> { { sndActivity, incOrEx } };
                    IncludeExcludes[fstActivity] = targets;
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

            if (Conditions.TryGetValue(fstActivity, out targets))
            {
                targets.Add(sndActivity);
            }
            else
            {
                Conditions.Add(fstActivity, new HashSet<Activity>() { sndActivity });
            }

        }

        public void RemoveCondition(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to remove relations from a Graph, that is Running. :$");
            
            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            HashSet<Activity> targets;

            if (Conditions.TryGetValue(fstActivity, out targets))
            {
                targets.Remove(sndActivity);
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

        public void MakeNestedGraph(HashSet<Activity> activities)
        {
            var nest = new Activity(activities.Aggregate("", (x, s) => x + s.Id),
                        "NestedGraph'" + activities.First().Name, this.Copy()); //TODO: we might get a problem from copying the graph as we actually need the same references for our relations
            
            List<String> toBeRemvActivities = new List<String>();

            foreach (var act in Activities)
            {
                if (activities.Contains(act)) //if it as an activity that should be in the inner graph.
                {
                    toBeRemvActivities.Add(act.Id);
                }
                else
                {
                    //should only 
                    nest.NestedGraph.RemoveActivityFromNest(act.Id);
                }
            }
            Activities.Add(nest
                );

            foreach (var act in toBeRemvActivities)
            {

                RemoveActivityFromOuterGraph(act, nest);
            }
        }

        public bool Execute(Activity a)
        {
            if(!Running)
                throw new InvalidOperationException("It is not permitted to execute an Activity on a Graph, that is not Running.");

            if(a.IsNestedGraph)
                throw new InvalidOperationException("not permitted to excute a nested graph");

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

        public static Dictionary<Activity, HashSet<Activity>> ConvertToDictionaryActivityHashSetActivity<T>(Dictionary<Activity, Dictionary<Activity, T>> inputDictionary)
        {
            var resultDictionary = new Dictionary<Activity, HashSet<Activity>>();
            foreach (var includeExclude in inputDictionary)
            {
                var source = includeExclude.Key;
                foreach (var keyValuePair in includeExclude.Value)
                {
                    var target = keyValuePair.Key; // .Value is a bool value which isn't used in the returned Dictionary
                    HashSet<Activity> targets;
                    resultDictionary.TryGetValue(source, out targets);
                    if (targets == null)
                    {
                        resultDictionary.Add(source, new HashSet<Activity> { target });
                    }
                    else
                    {
                        resultDictionary[source].Add(target);
                    }
                }
            }
            return resultDictionary;
        }

        public bool CanActivityEverBeIncluded(string id)
        {
            if (GetActivity(id).Included)
            {
                return true;
            }
            // Find potential includers
            var includedBy = IncludeExcludes.Where(incExc => incExc.Value.Any(target => target.Value && target.Key.Equals(GetActivity(id))));

            return includedBy.Any(keyValuePair => CanActivityEverBeIncluded(keyValuePair.Key.Id));
        }

        public bool ActivityHasRelations(Activity a)
        {
            return InRelation(a, IncludeExcludes)
                   || InRelation(a, Responses)
                   || InRelation(a, Conditions)
                   || InRelation(a, Milestones);
        }

        public bool InRelation(Activity activity, Dictionary<Activity, HashSet<Activity>> dictionary)
        {
            return dictionary.ContainsKey(activity)
                   || (dictionary.Any(x => x.Value.Contains(activity)));
        }

        public bool InRelation<T>(Activity activity, Dictionary<Activity, Dictionary<Activity, T>> dictionary)
        {
            return dictionary.ContainsKey(activity)
                   || (dictionary.Any(x => x.Value.ContainsKey(activity)));
        }

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

        public static byte[] HashDcrGraph(DcrGraph graph)
        {
            var array = new byte[graph.Activities.Count];
            int i = 0;
            foreach (var act in graph.Activities)
            {
                array[i++] = HashActivity(act);
            }
            return array;
        }

        public static byte HashActivity(Activity activity)
        {
            byte b = (byte)(activity.Executed ? 100 : 0);

            b += (byte)(activity.Included ? 10 : 0);

            b += (byte)(activity.Pending ? 10 : 0);

            return b;
        }

        public DcrGraph Copy()
        {
            var newDcrGraph = new DcrGraph();

            // Activities
            newDcrGraph.Activities = CloneActivityHashSet(Activities);

            // Responses
            foreach (var response in Responses)
            {
                newDcrGraph.Responses.Add(response.Key.Copy(), CloneActivityHashSet(response.Value));
            }

            // Includes and Excludes
            foreach (var inclusionExclusion in IncludeExcludes)
            {
                var activityBoolCopy = inclusionExclusion.Value.ToDictionary(activityBool => activityBool.Key.Copy(), activityBool => activityBool.Value);
                newDcrGraph.IncludeExcludes.Add(inclusionExclusion.Key.Copy(), activityBoolCopy);
            }

            // Conditions
            foreach (var condition in Conditions)
            {
                newDcrGraph.Conditions.Add(condition.Key.Copy(), CloneActivityHashSet(condition.Value));
            }

            // Milestones
            foreach (var milestone in Milestones)
            {
                newDcrGraph.Milestones.Add(milestone.Key.Copy(), CloneActivityHashSet(milestone.Value));
            }
            

            return newDcrGraph;
        }

        private HashSet<Activity> CloneActivityHashSet(HashSet<Activity> source)
        {
            var result = new HashSet<Activity>();
            foreach (var activity in source)
            {
                result.Add(activity.Copy());
            }
            return result;
        }

        public HashSet<Activity> GetIncludeOrExcludeRelation(Activity source, bool incl)
        {
            Dictionary<Activity, bool> dict;
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
                xml += String.Format(@"<event id=""{0}"" scope=""private"" >
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
                xml += String.Format(@"<label id =""{0}""/>", activity.Name);
                xml += "\n";
            }
            xml += "</labels>\n";
            // Label mappings
            xml += "<labelMappings>\n";
            foreach (var activity in Activities)
            {
                xml += String.Format(@"<labelMapping eventId =""{0}"" labelId = ""{1}""/>", activity.Id, activity.Name);
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
                    xml += String.Format(@"<exclude sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", condition.Key.Id, target.Id);
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
                    xml += String.Format(@"<response sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", response.Key.Id, target.Id);
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
                        xml += String.Format(@"<exclude sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", exclusion.Key.Id, target.Key.Id);
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
                        xml += String.Format(@"<include sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", inclusion.Key.Id, target.Key.Id);
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
                    xml += String.Format(@"<milestone sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", milestone.Key.Id, target.Id);
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
                    xml += String.Format(@"<event id=""{0}""/>", activity.Id);
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
                    xml += String.Format(@"<event id=""{0}""/>", activity.Id);
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
                    xml += String.Format(@"<event id=""{0}""/>", activity.Id);
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
                returnString += a + nl;
            }

            returnString += "\n Include-/exclude-relations: \n";

            var cnt = 0;
            foreach (var sourcePair in IncludeExcludes)
            {
                var source = sourcePair.Key;
                foreach (var targetPair in sourcePair.Value)
                {
                    var incOrEx = targetPair.Value ? " -->+ " : " -->% ";

                    returnString += source.Id + incOrEx + targetPair.Key.Id + "  |  " + (++cnt%6 == 0 ? nl : "");

                }
            }

            cnt = 0;
            returnString += "\n Responce-relations: \n";

            foreach (var sourcePair in Responses)
            {
                var source = sourcePair.Key;
                foreach (var target in sourcePair.Value)
                {
                    returnString += source.Id + " *--> " + target.Id + "  |  " + (++cnt % 6 == 0 ? nl : "");
                }
            }

            cnt = 0;
            returnString += "\n Condition-relations: \n";

            foreach (var sourcePair in Conditions)
            {
                var source = sourcePair.Key;
                foreach (var target in sourcePair.Value)
                {
                    returnString += source.Id + " -->* " + target.Id + "  |  " + (++cnt % 6 == 0 ? nl : "");
                }
            }

            cnt = 0;
            returnString += "\n Milestone-relations: \n";

            foreach (var sourcePair in Milestones)
            {
                var source = sourcePair.Key;
                foreach (var target in sourcePair.Value)
                {
                    returnString += source.Id + " --><> " + target.Id + "  |  " + (++cnt % 6 == 0 ? nl : "");
                }
            }

            return returnString;
        }

        #endregion
    }
}
