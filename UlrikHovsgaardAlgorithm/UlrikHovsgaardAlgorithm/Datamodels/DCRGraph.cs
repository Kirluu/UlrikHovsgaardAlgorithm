﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using UlrikHovsgaardAlgorithm.Datamodels;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class Confidence
    {
        public int Violations { get; set; } 
        public int Invocations { get; set; }
        public double Get { get { if (Invocations == 0) return 0; else return (double) Violations / Invocations; } }

        public bool IsAboveThreshold() => Get > Threshold.Value;

        public bool IncrInvocations()
        {
            var oldC = Get;
            Invocations++;
            var newC = Get;

            var t = Threshold.Value;
            return oldC <= t && t < newC || newC < t && t < oldC; // Raised above or fell below the threshold
        }

        public bool IncrViolations()
        {
            var oldC = Get;
            Violations++;
            var newC = Get;

            var t = Threshold.Value;
            return oldC <= t && t < newC || newC < t && t < oldC; // Raised above or fell below the threshold
        }

        /// <summary>
        /// Performs update to the confidence and returns whether the current threshold was passed in either direction
        /// </summary>
        public bool Increment (bool violationOccurred)
        {
            var oldC = Get;
            Invocations++;
            if (violationOccurred)
                Violations++;
            var newC = Get;

            // Check whether the confidence's update makes it go past the threshold
            var t = Threshold.Value;
            return oldC <= t && t < newC || newC <= t && t < oldC; // Raised above or fell below the threshold
        }

        public override string ToString()
        {
            return string.Format("{0} / {1} ({2:N2})", Violations, Invocations, Get);
        }
    }

    public class DcrGraph
    {
        #region Properties

        public string Title { get; set; }
        public HashSet<Activity> Activities { get; set; } = new HashSet<Activity>();
        public Dictionary<Activity, Dictionary<Activity,Confidence>> Responses { get; } = new Dictionary<Activity, Dictionary<Activity, Confidence>>();
        public Dictionary<Activity, Dictionary<Activity, Confidence>> IncludeExcludes { get; } = new Dictionary<Activity, Dictionary<Activity, Confidence>>(); // Confidense > threshold is include
        public Dictionary<Activity, Dictionary<Activity, Confidence>> Conditions { get; } = new Dictionary<Activity, Dictionary<Activity, Confidence>>();
        public Dictionary<Activity, Dictionary<Activity, Confidence>> Milestones { get; } = new Dictionary<Activity, Dictionary<Activity, Confidence>>();
        public int ConditionsCount
        {
            get => Conditions.Values.SelectMany(x => x).Count();
        }
        public int ResponsesCount
        {
            get => Responses.Values.SelectMany(x => x).Count();
        }
        public int IncludesCount
        {
            get => IncludeExcludes.Values.SelectMany(x => x.Where(y => y.Value.IsAboveThreshold())).Count();
        }
        public int ExcludesCount
        {
            get => IncludeExcludes.Values.SelectMany(x => x.Where(y => !y.Value.IsAboveThreshold())).Count();
        }

        public Dictionary<Activity, Confidence> PendingStates = new Dictionary<Activity, Confidence>();
        public Dictionary<Activity, Confidence> ExcludedStates = new Dictionary<Activity, Confidence>();

        public bool Running { get; set; }

        #endregion

        public Activity GetActivity(string id)
        {
            foreach (var act in Activities)
            {
                if (act.Id == id)
                    return act;
                if(act.IsNestedGraph)
                    foreach (var act2 in act.NestedGraph.Activities)
                    {
                        if (act2.Id == id)
                            return act2;
                    }
            }
            return null;
        }

        public int GetRelationCount => Responses.SelectMany(a => a.Value).Count()
                                       + Conditions.SelectMany(a => a.Value).Count()
                                       + Milestones.SelectMany(a => a.Value).Count()
                                       + IncludeExcludes.SelectMany(a => a.Value).Count();

        //gets activities INCLUDING THE NESTED ACTIVITIES, but not the nested graph.
        public HashSet<Activity> GetActivities()
        {
            HashSet<Activity> retrActivities = new HashSet<Activity>();

            foreach (var act in Activities.OrderBy(x => x.Id))
            {
                if (act.IsNestedGraph)
                    retrActivities.UnionWith(act.NestedGraph.GetActivities());
                else
                {
                    retrActivities.Add(act);
                }
            }
            return retrActivities;
        }

        //gets conditions INCLUDING THOSE IN NESTED GRAPHS
        public Dictionary<Activity, Dictionary<Activity, Confidence>> GetConditions()
        {
            // Get relations from this graph
            Dictionary<Activity, Dictionary<Activity, Confidence>> retrConditions;

            // Get relations from nested graphs
            var dictList = new List<Dictionary<Activity, Dictionary<Activity, Confidence>>> { Conditions };
            dictList.AddRange(from activity in Activities where activity.IsNestedGraph select activity.NestedGraph.GetConditions());
            // Merge all dictionaries
            try
            {
                retrConditions = dictList.SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            catch
            {
                // Duplicate key...
                MessageBox.Show("Duplicate key error!");
                return new Dictionary<Activity, Dictionary<Activity, Confidence>>();
            }

            return retrConditions;
        }

        public Dictionary<Activity, Dictionary<Activity, Confidence>> GetIncludesExcludes()
        {
            // Get relations from this graph
            Dictionary<Activity, Dictionary<Activity, Confidence>> retrIncludesExcludes;

            // Get relations from nested graphs
            var dictList = new List<Dictionary<Activity, Dictionary<Activity, Confidence>>> { IncludeExcludes };
            dictList.AddRange(from activity in Activities where activity.IsNestedGraph select activity.NestedGraph.GetIncludesExcludes());
            // Merge all dictionaries
            try
            {
                retrIncludesExcludes = dictList.SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            catch
            {
                // Duplicate key...
                MessageBox.Show("Duplicate key error!");
                return new Dictionary<Activity, Dictionary<Activity, Confidence>>();
            }

            return retrIncludesExcludes;
        }

        #region GraphBuilding methods
        
        public Activity AddActivity(string id, string name)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            var activity = new Activity(id, name);
            Activities.Add(activity);

            return activity;
        }

        public Activity AddActivity(string id, string name, string actor)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            var activity = new Activity(id, name) {Roles = actor};
            Activities.Add(activity);
            ExcludedStates[activity] = new Confidence();
            PendingStates[activity] = new Confidence();

            return activity;
        }

        public void AddActivities(params Activity[] activities)
        {

            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");


            foreach (var act in activities)
            {
                Activities.Add(act);
            }
        }

        public void AddActivity(string id, string name, DcrGraph nestedGraph)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            var act = new Activity(id, name, nestedGraph);
            Activities.Add(act);

            var otherActivities = Activities.Where(x => x != act).ToList();

            // Add relation confidences from this newly added activity to all others
            IncludeExcludes.Add(act, InitializeConfidenceMapping(otherActivities));
            Responses.Add(act, InitializeConfidenceMapping(otherActivities));
            Conditions.Add(act, InitializeConfidenceMapping(otherActivities));
            Milestones.Add(act, InitializeConfidenceMapping(otherActivities));

            // Also add relations from all other activities to this new one
            foreach (var other in otherActivities)
            {
                IncludeExcludes[other].Add(act, new Confidence());
                Responses[other].Add(act, new Confidence());
                Conditions[other].Add(act, new Confidence());
                Milestones[other].Add(act, new Confidence());
            }
        }
        private Dictionary<Activity, Confidence> InitializeConfidenceMapping(IEnumerable<Activity> activities)
        {
            var res = new Dictionary<Activity, Confidence>();
            foreach (var act in activities)
            {
                res.Add(act, new Confidence());
            }
            return res;
        }

        public void AddRolesToActivity(string id, string roles)
        {
            GetActivity(id).Roles = roles;
        }

        //Is used to remove the activity and then change the target of other relations to the Nested graph instead.
        public void RemoveActivityFromOuterGraph(string id, Activity nest)
        {
            throw new NotImplementedException(); //problems since we cannot support inner graphs with statistics.

            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activity act = GetActivity(id);

            Activities.RemoveWhere(a => a.Id == id);

            foreach (var sourceTargetPair in Responses) 
            {
                if (sourceTargetPair.Value.ContainsKey(act) && !nest.NestedGraph.Activities.Contains(sourceTargetPair.Key))
                {
                    //todo: do we trust our activity equality?
                    sourceTargetPair.Value.Remove(act);
                    sourceTargetPair.Value.Add(nest,new Confidence());
                }

            }
            foreach (var sourceTargetPair in Conditions)
            {
                if (sourceTargetPair.Value.ContainsKey(act) && !nest.NestedGraph.Activities.Contains(sourceTargetPair.Key))
                {
                    sourceTargetPair.Value.Remove(act);
                    sourceTargetPair.Value.Add(nest,new Confidence());
                }

            }
            foreach (var sourceTargetPair in Milestones)
            {
                if (sourceTargetPair.Value.ContainsKey(act) && !nest.NestedGraph.Activities.Contains(sourceTargetPair.Key))
                {
                    sourceTargetPair.Value.Remove(act);
                    sourceTargetPair.Value.Add(nest,new Confidence());
                }

            }
            foreach (var sourceTargetPair in IncludeExcludes)
            {
                Confidence inOrEx;
                if (sourceTargetPair.Value.TryGetValue(act, out inOrEx) && !nest.NestedGraph.Activities.Contains(sourceTargetPair.Key))
                {
                    sourceTargetPair.Value.Remove(act);
                    sourceTargetPair.Value[nest] = inOrEx;
                }
            }
        }

        /// <summary>
        /// Removes an activity and all the relations involving it from the DcrGraph.
        /// </summary>
        /// <param name="id">ID of the activity to remove from the graph.</param>
        /// <returns>The amount of relations that were removed by effect of removing the activity and all involved relations.</returns>
        public int RemoveActivity(string id)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activity act = GetActivity(id);

            Activities.RemoveWhere(a => a.Id == id);

            return RemoveFromRelation(Responses, act)
                + RemoveFromRelation(Conditions, act)
                + RemoveFromRelation(Milestones, act)
                + RemoveFromRelation(IncludeExcludes, act);
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

        private static int RemoveFromRelation(Dictionary<Activity, Dictionary<Activity, Confidence>> relation, Activity act)
        {
            var removedRelations = 0;
            // All relations where act is a target
            foreach (var source in relation)
            {
                if (source.Value.Remove(act))
                    removedRelations++;
            }

            // All outgoing relations
            removedRelations += relation.TryGetValue(act, out var bla) ? bla.Count : 0;
            relation.Remove(act);

            return removedRelations;
        }

        //private static void RemoveFromRelation(Dictionary<Activity, Dictionary<Activity, bool>> incExRelation, Activity act)
        //{
        //    foreach (var source in incExRelation)
        //    {
        //        source.Value.Remove(act);
        //    }
        //    incExRelation.Remove(act);
        //}


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


        public void AddIncludeExclude(bool incl, string firstId, string secondId)
        {
            AddIncludeExclude(incl ? new Confidence() {Invocations = 1, Violations = 1} : new Confidence(), 
                firstId,
                secondId);
        }

        private bool AddIncludeExclude(Confidence confidence, string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");
            

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity, Confidence> targets;

            if (IncludeExcludes.TryGetValue(fstActivity, out targets))
            {
                targets.Add(sndActivity, confidence);
                return true;
            }
            else
            {
                var dic = new Dictionary<Activity, Confidence>();
                dic[sndActivity] = confidence;

                IncludeExcludes.Add(fstActivity, dic);
                return true;
            }
        }

        //addresponce Condition and milestone should probably be one AddRelation method, that takes an enum.
        public bool AddResponse(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            if (firstId == secondId) //because responce to one self is not healthy.
                return false;

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity,Confidence> targets;

            if (Responses.TryGetValue(fstActivity, out targets))
            {
                targets.Add(sndActivity,new Confidence());
                return true;
            }
            else
            {
                var dic = new Dictionary<Activity, Confidence>();
                dic[sndActivity] = new Confidence();
                
                Responses.Add(fstActivity, dic);
                return true;
            }
        }

        public bool AddCondition(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");


            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity, Confidence> targets;

            if (Conditions.TryGetValue(fstActivity, out targets))
            {
                 targets.Add(sndActivity,new Confidence());
                return true;
            }
            else
            {
                var dic = new Dictionary<Activity, Confidence>();
                dic[sndActivity] = new Confidence();

                Conditions.Add(fstActivity, dic);
                return true;
            }
        }

        public void RemoveCondition(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to remove relations from a Graph, that is Running. :$");
            
            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity, Confidence> targets;

            if (Conditions.TryGetValue(fstActivity, out targets))
            {
                targets.Remove(sndActivity);
            }
        }

        public bool AddMileStone(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            if (firstId == secondId) //because Milestone to one self is not healthy.
                return false;

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity, Confidence> targets;

            if (Milestones.TryGetValue(fstActivity, out targets))
            {
                targets.Add(sndActivity, new Confidence());
                return true;
            }
            else
            {
                var dic = new Dictionary<Activity, Confidence>();
                dic[sndActivity] = new Confidence();

                Milestones.Add(fstActivity, dic);
                return true;
            }
        }

        public void RemoveIncludeExclude(string firstId, string secondId)
        {
            if (Running)
                throw new InvalidOperationException("It is not permitted to add relations to a Graph, that is Running. :$");

            Activity fstActivity = GetActivity(firstId);
            Activity sndActivity = GetActivity(secondId);

            Dictionary<Activity, Confidence> targets;

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

                //used to store the dictionary before filtering out the relations based on threshold
                Dictionary<Activity, Confidence> unfiltered;

                //and no other included and non-executed activity has a condition to it
                if (!source.Executed && Conditions.TryGetValue(source, out unfiltered))
                {
                    targets = FilterDictionaryByThreshold(unfiltered);

                    var nestedTargets = targets.Where(a => a.IsNestedGraph);

                    foreach (var targ in nestedTargets)
                    {
                        conditionTargets.UnionWith(targ.NestedGraph.Activities);
                    }

                    conditionTargets.UnionWith(targets);
                }

                //and no other included and pending activity has a milestone relation to it.
                if (source.Pending && Milestones.TryGetValue(source, out unfiltered))
                {

                    targets = FilterDictionaryByThreshold(unfiltered);


                    var nestedTargets = targets.Where(a => a.IsNestedGraph);

                    foreach (var targ in nestedTargets)
                    {
                        conditionTargets.UnionWith(targ.NestedGraph.Activities);
                    }

                    conditionTargets.UnionWith(targets);
                }

            }

            included.ExceptWith(conditionTargets);

            return included;
        }

        public void MakeNestedGraph(HashSet<Activity> activities)
        {
            MakeNestedGraph(activities.Aggregate("", (acc, a) => acc + a.Id), "NestedGraph " + activities.Aggregate("", (acc, a) => acc + a.Id), activities);
        }

        public Activity MakeNestedGraph(string id, string name, HashSet<Activity> activities)
        {
            var nest = new Activity(id, name, this.Copy()); 

            List<String> toBeRemvActivities = new List<String>();

            foreach (var act in Activities)
            {
                if (activities.Contains(act)) //if it as an activity that should be in the inner graph.
                {
                    toBeRemvActivities.Add(act.Id);
                }
                else
                {
                    //should only remove from nested graph
                    nest.NestedGraph.RemoveActivityFromNest(act.Id);
                }
            }
            Activities.Add(nest);

            // TODO: Commented out because inner graphs don't work with statistics atm.
            //foreach (var act in toBeRemvActivities)
            //{
            //    RemoveActivityFromOuterGraph(act, nest);
            //}

            return nest;
        }

        public bool Execute(Activity a)
        {
            if(!Running)
                throw new InvalidOperationException("It is not permitted to execute an Activity on a Graph, that is not Running.");

            if(a == null)
                throw new ArgumentNullException();

            if(a.IsNestedGraph)
                throw new InvalidOperationException("not permitted to excute a nested graph");

            //if the activity is not runnable
            if (!GetRunnableActivities().Contains(a))
                return false;

            //var act = GetActivity(a.Id); 
            var act = a;

            //the activity is now executed
            act.Executed = true;

            //it is not pending
            act.Pending = false;

            //its response relations are now pending.
            Dictionary<Activity,Confidence> respTargets;
            if (Responses.TryGetValue(act, out respTargets))
            {
                foreach (var respActivity in FilterDictionaryByThreshold(respTargets))
                {
                    GetActivity(respActivity.Id).Pending = true;
                }
            }

            //its include/exclude relations are now included/excluded.
            Dictionary<Activity, Confidence> incExcTargets;
            if (IncludeExcludes.TryGetValue(act, out incExcTargets)) 
            {
                foreach (var keyValuePair in incExcTargets)
                {
                    var isInclusion = keyValuePair.Value.Get > Threshold.Value;
                    GetActivity(keyValuePair.Key.Id).Included = isInclusion;
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
            HashSet<Activity> activitiesToReturn = new HashSet<Activity>();

            foreach (var act in Activities)
            {
                if (act.IsNestedGraph)
                {
                    activitiesToReturn.UnionWith(act.NestedGraph.GetIncludedActivities());
                }
                else if (act.Included)
                {
                    activitiesToReturn.Add(act);
                }
            }

            return activitiesToReturn;
        }

        #endregion

        #region Utilitary methods (IsEqualState, Copy, ExportToXml, ToString)


        public static HashSet<Activity> FilterDictionaryByThreshold(Dictionary<Activity, Confidence> dictionary)
        {
            return new HashSet<Activity>(dictionary.Where(ac => ac.Value.Get <= Threshold.Value).Select(a => a.Key));
        }

        public static Dictionary<Activity, Confidence> FilterDictionaryByThresholdAsDictionary(Dictionary<Activity, Confidence> dictionary)
        {
            return
                new Dictionary<Activity, Confidence>(
                    dictionary.Where(ac => ac.Value.Get <= Threshold.Value).ToDictionary(x => x.Key, x => x.Value));
        }

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
            var includedBy = IncludeExcludes.Where(incExc => incExc.Value.Any(target => (target.Value.Get > Threshold.Value) && target.Key.Equals(GetActivity(id))));

            return includedBy.Any(keyValuePair => CanActivityEverBeIncluded(keyValuePair.Key.Id));
        }

        public bool ActivityHasRelations(Activity a)
        {
            return InRelation(a, IncludeExcludes, true)
                   || InRelation(a, Responses, false)
                   || InRelation(a, Conditions, false)
                   || InRelation(a, Milestones, false);
        }

        public bool InRelation(Activity activity, Dictionary<Activity, HashSet<Activity>> dictionary)
        {
            return dictionary.Any(x => Equals(x.Key, activity) && x.Value.Any())
                   || (dictionary.Any(x => x.Value.Contains(activity)));
        }

        public bool InRelation(Activity activity, Dictionary<Activity, Dictionary<Activity, Confidence>> dictionary, bool isIncludeExclude)
        {
            return
                // any outgoing relations?
                dictionary.Any(x => Equals(x.Key, activity) && (isIncludeExclude && x.Value.Any() || x.Value.Any(y => !y.Value.IsAboveThreshold()))) // Any not above threshold - any that is not contradicted - any relations
                // any incoming relations?
                || (dictionary.Any(x => x.Value.ContainsKey(activity) && x.Value[activity].IsAboveThreshold()));
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
                    activity.Pending != comparedActivity.Pending ||  
                    ((activity.IsNestedGraph && comparedActivity.IsNestedGraph)
                        ? !activity.NestedGraph.AreInEqualState(comparedActivity.NestedGraph)
                        : (activity.IsNestedGraph != comparedActivity.IsNestedGraph)))
                {
                    return false;
                }
            }
            return true;
        }

        public static byte[] HashDcrGraph(DcrGraph graph)
        {
            var array = new byte[graph.GetActivities().Count];
            int i = 0;
            var runnables = graph.GetRunnableActivities();
            foreach (var act in graph.GetActivities().OrderBy(x => x.Id))
            {
                array[i++] = act.HashActivity(runnables.Contains(act));
            }
            return array;
        }

        public DcrGraph Copy()
        {
            var newDcrGraph = new DcrGraph();

            // Activities
            newDcrGraph.Activities = CloneActivityHashSet(Activities);

            // Responses
            foreach (var response in Responses)
            {
                var activityConfidenceCopy = response.Value.ToDictionary(activityBool => activityBool.Key.Copy(), activityBool => activityBool.Value);
                newDcrGraph.Responses.Add(response.Key.Copy(), activityConfidenceCopy);
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
                var activityConfidenceCopy = condition.Value.ToDictionary(activityBool => activityBool.Key.Copy(), activityBool => activityBool.Value);
                newDcrGraph.Conditions.Add(condition.Key.Copy(), activityConfidenceCopy);
            }

            // Milestones
            foreach (var milestone in Milestones)
            {
                var activityConfidenceCopy = milestone.Value.ToDictionary(activityBool => activityBool.Key.Copy(), activityBool => activityBool.Value);
                newDcrGraph.Milestones.Add(milestone.Key.Copy(), activityConfidenceCopy);
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
            Dictionary<Activity, Confidence> dict;
            if (IncludeExcludes.TryGetValue(source, out dict))
            {
            HashSet<Activity> set = new HashSet<Activity>();

                foreach (var target in dict)
            {
                if ((target.Value.Get > Threshold.Value) == incl)
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
                    var incOrEx = targetPair.Value.Get > Threshold.Value ? " -->+ " : " -->% ";

                    returnString += source.Id + incOrEx + targetPair.Key.Id + "  |  " + (++cnt%6 == 0 ? nl : "");
                }
            }

            cnt = 0;
            returnString += "\n Responce-relations: \n";

            foreach (var sourcePair in Responses)
            {
                var source = sourcePair.Key;
                foreach (var target in FilterDictionaryByThreshold(sourcePair.Value))
                {
                    returnString += source.Id + " *--> " + target.Id + "  |  " + (++cnt % 6 == 0 ? nl : "");
                }
            }

            cnt = 0;
            returnString += "\n Condition-relations: \n";

            foreach (var sourcePair in Conditions)
            {
                var source = sourcePair.Key;
                foreach (var target in FilterDictionaryByThreshold(sourcePair.Value))
                {
                    returnString += source.Id + " -->* " + target.Id + "  |  " + (++cnt % 6 == 0 ? nl : "");
                }
            }

            cnt = 0;
            returnString += "\n Milestone-relations: \n";

            foreach (var sourcePair in Milestones)
            {
                var source = sourcePair.Key;
                foreach (var target in FilterDictionaryByThreshold(sourcePair.Value))
                {
                    returnString += source.Id + " --><> " + target.Id + "  |  " + (++cnt % 6 == 0 ? nl : "");
                }
            }

            return returnString;
        }

        #endregion
    }
}
