using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UlrikHovsgaardAlgorithm.Datamodels;

namespace UlrikHovsgaardAlgorithm.Data
{
    /// <summary>
    /// A compressed DcrGraph implementation, that can only run activities, and which cannot be dynamically built
    /// </summary>
    public class ByteDcrGraph
    {
        public Dictionary<int, string> IndexToActivityId { get; } = new Dictionary<int, string>();
        public Dictionary<string, int> ActivityIdToIndex { get; } = new Dictionary<string, int>();

        public byte[] State { get; private set; } 
        public Dictionary<int, HashSet<int>> Includes { get; private set; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> Excludes { get; private set; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> Responses { get; private set; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> ConditionsReversed { get; private set; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> MilestonesReversed { get; private set; } = new Dictionary<int, HashSet<int>>();


        public ByteDcrGraph(DcrGraph inputGraph, ByteDcrGraph comparisonGraph = null)
        {
            if (inputGraph.Activities.Count < 8)
            {
                int i = 0;
            }

            State = DcrGraph.HashDcrGraph(inputGraph, comparisonGraph);

            // Store the activities' IDs for potential lookup later
            if (comparisonGraph != null)
            {
                // Use same mappings
                IndexToActivityId = comparisonGraph.IndexToActivityId;
                ActivityIdToIndex = comparisonGraph.ActivityIdToIndex;
            }
            else
            {
                var activityList = inputGraph.GetActivities().OrderBy(x => x.Id).ToList();
                for (int i = 0; i < activityList.Count; i++)
                {
                    IndexToActivityId.Add(i, activityList[i].Id);
                    ActivityIdToIndex.Add(activityList[i].Id, i);
                }
            }

            // Set up relations
            foreach (var inclExcl in inputGraph.IncludeExcludes)
            {
                int source = ActivityIdToIndex[inclExcl.Key.Id];
                foreach (var targetPair in inclExcl.Value)
                {
                    //int target = ActivityIdToIndex[keyValuePair.Key.Id];

                    // Fetch potentially nested targets as well - if not nested, only that activity itself TODO: Should be similar nesting-logic for all relation-types
                    var targets =
                        (targetPair.Key.IsNestedGraph
                            ? targetPair.Key.NestedGraph.Activities
                            : new HashSet<Activity> {targetPair.Key})
                        .Select(x => ActivityIdToIndex[x.Id]);
                    
                    if (targetPair.Value.Get > Threshold.Value)
                    {
                        // INCLUSION
                        foreach (var target in targets)
                        {
                            if (Includes.ContainsKey(source))
                            {
                                Includes[source].Add(target);
                            }
                            else
                            {
                                Includes.Add(source, new HashSet<int> {target});
                            }
                        }
                    }
                    else // EXCLUSION
                    {
                        foreach (var target in targets)
                        {
                            if (Excludes.ContainsKey(source))
                            {
                                Excludes[source].Add(target);
                            }
                            else
                            {
                                Excludes.Add(source, new HashSet<int> { target });
                            }
                        }
                    }
                }
            }

            foreach (var response in inputGraph.Responses)
            {
                int source = ActivityIdToIndex[response.Key.Id];

                foreach (var target in DcrGraph.FilterDictionaryByThreshold(response.Value).Where(a => !a.IsNestedGraph)
                    .Union(DcrGraph.FilterDictionaryByThreshold(response.Value).Where(a => a.IsNestedGraph)
                        .SelectMany(a => a.NestedGraph.Activities)))
                {
                    var targetIdx = ActivityIdToIndex[target.Id];
                    if (Responses.ContainsKey(source))
                    {
                        Responses[source].Add(targetIdx);
                    }
                    else
                    {
                        Responses.Add(source, new HashSet<int> { targetIdx });
                    }
                }
            }

            foreach (var condition in inputGraph.Conditions)
            {
                int source = ActivityIdToIndex[condition.Key.Id];
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(condition.Value)
                    .Where(a => !a.IsNestedGraph).Union(DcrGraph.FilterDictionaryByThreshold(condition.Value)
                        .Where(a => a.IsNestedGraph).SelectMany(a => a.NestedGraph.Activities)))
                {
                    var targetIdx = ActivityIdToIndex[target.Id];
                    if (ConditionsReversed.ContainsKey(targetIdx))
                    {
                        ConditionsReversed[targetIdx].Add(source);
                    }
                    else
                    {
                        ConditionsReversed.Add(targetIdx, new HashSet<int> { source });
                    }
                }
            }

            foreach (var milestone in inputGraph.Milestones)
            {
                int source = ActivityIdToIndex[milestone.Key.Id];
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(milestone.Value).Where(a => !a.IsNestedGraph).Union(DcrGraph.FilterDictionaryByThreshold(milestone.Value).Where(a => a.IsNestedGraph).SelectMany(a => a.NestedGraph.Activities)))
                {
                    var targetIdx = ActivityIdToIndex[target.Id];
                    if (MilestonesReversed.ContainsKey(targetIdx))
                    {
                        MilestonesReversed[targetIdx].Add(source);
                    }
                    else
                    {
                        MilestonesReversed.Add(targetIdx, new HashSet<int> { source });
                    }
                }
            }
        }

        public ByteDcrGraph(DcrGraphSimple inputGraph, ByteDcrGraph comparisonGraph)
        {
            State = DcrGraphSimple.HashDcrGraph(inputGraph, comparisonGraph);
            
            // Store the activities' IDs for potential lookup later
            if (comparisonGraph != null) // If given, use same mapping!
            {
                IndexToActivityId = comparisonGraph.IndexToActivityId;
                ActivityIdToIndex = comparisonGraph.ActivityIdToIndex;
            }
            else
            {
                var activityList = inputGraph.Activities.OrderBy(x => x.Id).ToList();
                for (int i = 0; i < activityList.Count; i++)
                {
                    IndexToActivityId.Add(i, activityList[i].Id);
                    ActivityIdToIndex.Add(activityList[i].Id, i);
                }
            }

            // Set up relations
            foreach (var incl in inputGraph.Includes)
            {
                int source = ActivityIdToIndex[incl.Key.Id];
                foreach (var unfilteredTargets in incl.Value)
                {
                    var targets =
                    (unfilteredTargets.IsNestedGraph
                        ? unfilteredTargets.NestedGraph.Activities
                        : new HashSet<Activity> {unfilteredTargets}).Select(a => ActivityIdToIndex[a.Id]);
                    
                    foreach (var target in targets)
                    {
                        if (Includes.ContainsKey(source))
                        {
                            Includes[source].Add(target);
                        }
                        else
                        {
                            Includes.Add(source, new HashSet<int> { target });
                        }
                    }
                }
            }

            foreach (var excl in inputGraph.Excludes)
            {
                int source = ActivityIdToIndex[excl.Key.Id];
                foreach (var keyValuePair in excl.Value)
                {
                    var targets =
                    (keyValuePair.IsNestedGraph
                        ? keyValuePair.NestedGraph.Activities
                        : new HashSet<Activity> {keyValuePair}).Select(a => ActivityIdToIndex[a.Id]);
                    
                    foreach (var target in targets)
                    {
                        if (Excludes.ContainsKey(source))
                        {
                            Excludes[source].Add(target);
                        }
                        else
                        {
                            Excludes.Add(source, new HashSet<int> { target });
                        }
                    }
                }
            }

            foreach (var response in inputGraph.Responses)
            {
                int source = ActivityIdToIndex[response.Key.Id];
                foreach (var target in response.Value.Where(a => !a.IsNestedGraph).Union(response.Value.Where(a => a.IsNestedGraph).SelectMany(a => a.NestedGraph.Activities)))
                {
                    var targetIdx = ActivityIdToIndex[target.Id];
                    if (Responses.ContainsKey(source))
                    {
                        Responses[source].Add(targetIdx);
                    }
                    else
                    {
                        Responses.Add(source, new HashSet<int> { targetIdx });
                    }
                }
            }

            foreach (var condition in inputGraph.Conditions)
            {
                int source = ActivityIdToIndex[condition.Key.Id];
                foreach (var target in condition.Value.Where(a => !a.IsNestedGraph).Union(condition.Value.Where(a => a.IsNestedGraph).SelectMany(a => a.NestedGraph.Activities)))
                {
                    var targetIdx = ActivityIdToIndex[target.Id];
                    if (ConditionsReversed.ContainsKey(targetIdx))
                    {
                        ConditionsReversed[targetIdx].Add(source);
                    }
                    else
                    {
                        ConditionsReversed.Add(targetIdx, new HashSet<int> { source });
                    }
                }
            }
        }

        public ByteDcrGraph Copy()
        {
            return new ByteDcrGraph(this);
        }

        private ByteDcrGraph(ByteDcrGraph copyFrom)
        {
            // Deep copy of state
            State = new byte[copyFrom.State.Length];
            copyFrom.State.CopyTo(State, 0);

            // Shallow copy of Id correspondences
            IndexToActivityId = copyFrom.IndexToActivityId;
            ActivityIdToIndex = copyFrom.ActivityIdToIndex;

            // Shallow copy of relations
            Includes = copyFrom.Includes;
            Excludes = copyFrom.Excludes;
            Responses = copyFrom.Responses;
            ConditionsReversed = copyFrom.ConditionsReversed;
            MilestonesReversed = copyFrom.MilestonesReversed;
        }

        public void RemoveActivity(string id)
        {
            var index = ActivityIdToIndex[id];
            
            State[index] = 0;
            Includes = Includes.ToDictionary(v => v.Key, v => new HashSet<int>(v.Value.Where(ac => ac != index)));

            // NEW, more thorough approach for removal of activity (ruins traces atm., when reporting error-trace):

            //// Redefine state WITHOUT the activity
            //State = State.Select((b, idx) => new {b, idx}).Where(s => s.idx != index).Select(s => s.b).ToArray();

            //// Remove from all collections:
            //Includes = RemoveIndexFromCollection(Includes, index);
            //Excludes = RemoveIndexFromCollection(Excludes, index);
            //Responses = RemoveIndexFromCollection(Responses, index);
            //ConditionsReversed = RemoveIndexFromCollection(ConditionsReversed, index);
            //MilestonesReversed = RemoveIndexFromCollection(MilestonesReversed, index);

            //// Finally, update ID-to-index mapping to only map to the new amount of indices left:
            //// All indices after the one we removed have to be "moved" one up
            //var indicesAfter = Enumerable.Range(index + 1, ActivityIdToIndex.Count - index - 1);
            //var actIdsAfter = indicesAfter.Select(idx => IndexToActivityId[idx]);

            //// Decrease mappings to indexes located AFTER the one being removed:
            //foreach (var actId in actIdsAfter)
            //{
            //    ActivityIdToIndex[actId]--;
            //}
            //ActivityIdToIndex.Remove(id); // Remove actual activity ID being removed from graph

            //// Rebuild inversed map using new, valid map:
            //IndexToActivityId.Clear();
            //foreach (var kv in ActivityIdToIndex)
            //{
            //    IndexToActivityId.Add(kv.Value, kv.Key);
            //}
        }

        private static Dictionary<int, HashSet<int>> RemoveIndexFromCollection(Dictionary<int, HashSet<int>> dict, int index)
        {
            // Remove imperatively (single-core performance, since nothing else is thread-safe anyways):
            dict.Remove(index);
            foreach (var kv in dict.ToDictionary(x => x.Key, x => x.Value)) // Cloned dictionary to allow modifications to given one
            {
                kv.Value.Remove(index);
                foreach (var val in kv.Value.OrderBy(x => x).ToList()) // Ordered and cloned for secure updates
                {
                    // For all mappings to indices larger than the removed one - decrease that index by one (remove and re-add)
                    if (val > index)
                    {
                        dict[kv.Key].Remove(val);
                        dict[kv.Key].Add(val - 1);
                    }
                }
            }
            return dict;

            // Approach which creates NEW map:
            return dict.Where(x => x.Key != index).ToDictionary(v => v.Key, v => new HashSet<int>(v.Value.Where(ac => ac != index)));
        }

        public List<int> GetRunnableIndexes()
        {
            var resList = new List<int>();
            for (int i = 0; i < State.Length; i++)
            {
                if (CanByteRun(State[i]))
                {
                    resList.Add(i);
                }
            }
            return resList;
        }

        public static byte[] StateWithExcludedActivitiesEqual(byte[] b)
        {
            var retB = new byte[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                retB[i] = (byte) (IsByteIncluded(b[i]) ? b[i] : 0);
            }
            return retB;
        }

        private bool ActivityCanRun(int idx)
        {
            if (!IsByteIncluded(State[idx])) return false;
            if (ConditionsReversed.ContainsKey(idx))
            {
                if (!ConditionsReversed[idx].All(source => IsByteExcludedOrExecuted(State[source])))
                {
                    return false;
                }
            }
            // No milestone --> Activity can run
            if (!MilestonesReversed.ContainsKey(idx)) return true;
            // If all milestones are accepting
            return MilestonesReversed[idx].All(source => IsByteExcludedOrNotPending(State[source]));
        }

        /// <summary>
        /// ASSUMES that idx is a runnable activity!
        /// </summary>
        /// <param name="idx">The byte array index of the activity to be executed</param>
        public void ExecuteActivity(int idx)
        {
            // Executed = true
            State[idx] = (byte)((State[idx]) | (1 << 2));
            // Pending = false
            State[idx] = (byte)((State[idx]) & (1 ^ Byte.MaxValue));

            // Execute Includes, Excludes, Responses
            if (Includes.ContainsKey(idx))
            {
                foreach (var inclusion in Includes[idx]) // Outgoing Includes
                {
                    SetActivityIncludedExcluded(true, inclusion);
                }
            }
            if (Excludes.ContainsKey(idx))
            {
                foreach (var exclusion in Excludes[idx]) // Outgoing Includes
                {
                    SetActivityIncludedExcluded(false, exclusion);
                }
            }
            if (Responses.ContainsKey(idx))
            {
                foreach (var response in Responses[idx]) // Outgoing Includes
                {
                    SetActivityPending(response);
                }
            }

            for (int i = 0; i < State.Length; i++)
            {
                if (ActivityCanRun(i))
                {
                    SetActivityRunnable(i);
                }
                else
                {
                    SetActivityNotRunnable(i);
                }
            }
        }

        private void SetActivityIncludedExcluded(bool include, int idx)
        {
            if (include)
            {
                // Included = true
                State[idx] = (byte)((State[idx]) | (1 << 1));
            }
            else
            {
                // Included = false
                State[idx] = (byte)((State[idx]) & ((1 << 1) ^ Byte.MaxValue));
                
            }
        }

        public static bool IsFinalState(byte[] state) // OK
        {
            // Mustn't be an activity which is both Pending and Included
            return !state.Any(t => IsByteIncluded(t) && IsBytePending(t));
        }

        private void SetActivityPending(int idx) // OK
        {
            // Pending = true
            State[idx] = (byte)((State[idx]) | 1);
        }

        private void SetActivityRunnable(int idx)
        {
            State[idx] = (byte) ((State[idx]) | (1 << 3));
        }

        private void SetActivityNotRunnable(int idx)
        {
            State[idx] = (byte)((State[idx]) & ((1 << 3) ^ Byte.MaxValue));
        }

        public static bool IsByteIncluded(byte b)
        {
            return (b & (1 << 1)) > 0;
        }

        public static bool CanByteRun(byte b)
        {
            return (b & (1 << 3)) > 0;
        }

        public static bool IsBytePending(byte b)
        {
            return (b & 1) > 0;
        }

        // FALSE = Condition is binding and the target cannot execute
        public static bool IsByteExcludedOrExecuted(byte b)
        {
            return (b & (1 << 1)) <= 0 || (b & (1 << 2)) > 0;
        }

        public static bool IsByteExcludedOrNotPending(byte b)
        {
            return (b & (1 << 1)) <= 0 || (b & 1) <= 0;
        }

    }
}
