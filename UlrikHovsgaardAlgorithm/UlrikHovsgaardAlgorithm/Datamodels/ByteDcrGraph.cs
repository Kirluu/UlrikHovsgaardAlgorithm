using System;
using System.Collections.Generic;
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
        public Dictionary<string, string> IndexIdToActivityId { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> ActivityIdToIndexId { get; } = new Dictionary<string, string>();

        public byte[] State { get; } 
        public Dictionary<int, HashSet<int>> Includes { get; set; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> Excludes { get; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> Responses { get; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> ConditionsReversed { get; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> MilestonesReversed { get; } = new Dictionary<int, HashSet<int>>();


        public ByteDcrGraph(DcrGraph inputGraph)
        {
            State = DcrGraph.HashDcrGraph(inputGraph);

            var activityList = inputGraph.GetActivities().ToList();

            // Store the activities' IDs for potential lookup later
            for (int i = 0; i < activityList.Count; i++)
            {
                IndexIdToActivityId.Add(i.ToString(), activityList[i].Id);
                ActivityIdToIndexId.Add(activityList[i].Id, i.ToString());
            }

            // Set up relations
            foreach (var inclExcl in inputGraph.IncludeExcludes)
            {
                var source = activityList.FindIndex(a => a.Equals(inclExcl.Key));
                foreach (var keyValuePair in inclExcl.Value)
                {
                    var targets = (keyValuePair.Key.IsNestedGraph ? keyValuePair.Key.NestedGraph.Activities : new HashSet<Activity>() {keyValuePair.Key}).Select(x => activityList.FindIndex(a => a.Equals(x)));
                    
                    if (keyValuePair.Value.IsContradicted())
                    {

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
                    else
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
                var source = activityList.FindIndex(a => a.Equals(response.Key));
                
                
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(response.Value).Where(a => !a.IsNestedGraph).Union(DcrGraph.FilterDictionaryByThreshold(response.Value).Where(a => a.IsNestedGraph).SelectMany(a => a.NestedGraph.Activities)))
                {
                    var targetIdx = activityList.FindIndex(a => a.Equals(target));
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
                var source = activityList.FindIndex(a => a.Equals(condition.Key));
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(condition.Value).Where(a => !a.IsNestedGraph).Union(DcrGraph.FilterDictionaryByThreshold(condition.Value).Where(a => a.IsNestedGraph).SelectMany(a => a.NestedGraph.Activities)))
                {
                    var targetIdx = activityList.FindIndex(a => a.Equals(target));
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
                var source = activityList.FindIndex(a => a.Equals(milestone.Key));
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(milestone.Value).Where(a => !a.IsNestedGraph).Union(DcrGraph.FilterDictionaryByThreshold(milestone.Value).Where(a => a.IsNestedGraph).SelectMany(a => a.NestedGraph.Activities)))
                {
                    var targetIdx = activityList.FindIndex(a => a.Equals(target));
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

        public ByteDcrGraph(ByteDcrGraph byteDcrGraph)
        {
            // Deep copy of state
            State = new byte[byteDcrGraph.State.Length];
            byteDcrGraph.State.CopyTo(State, 0);

            // Shallow copy of Id correspondences
            IndexIdToActivityId = byteDcrGraph.IndexIdToActivityId;
            ActivityIdToIndexId = byteDcrGraph.ActivityIdToIndexId;

            // Shallow copy of relations
            Includes = byteDcrGraph.Includes;
            Excludes = byteDcrGraph.Excludes;
            Responses = byteDcrGraph.Responses;
            ConditionsReversed = byteDcrGraph.ConditionsReversed;
            MilestonesReversed = byteDcrGraph.MilestonesReversed;
        }

        public void RemoveActivity(string id)
        {
            var intID = Int32.Parse(ActivityIdToIndexId[id]);

            State[intID] = 0;

            Includes = Includes.ToDictionary(v => v.Key,v => new HashSet<int>(v.Value.Where(ac => ac != intID)));

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

        public  byte[] StateWithNonRunnableActivitiesEqual(byte[] b)
        {
            var retB = new byte[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                retB[i] = (byte) (CanByteRun(b[i]) ? b[i] : 0);
            }
            return retB;
        }



        private bool ActivityCanRun(int idx) {
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
            State[idx] = (byte)((State[idx]) | 1<<2);
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

        public static bool IsFinalState(byte[] state)
        {
            // Mustn't be an activity which is both Pending and Included
            return !state.Any(t => IsByteIncluded(t) && IsBytePending(t));
        }

        private void SetActivityPending(int idx)
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
            return (b & 1 << 1) > 0;
        }

        public static bool CanByteRun(byte b)
        {
            return (b & 1 << 3) > 0;
        }

        public static bool IsBytePending(byte b)
        {
            return (b & 1) > 0;
        }

        // FALSE = Condition is binding and the target cannot execute
        public static bool IsByteExcludedOrExecuted(byte b)
        {
            return (b & 1 << 1) <= 0 || (b & 1 << 2) > 0;
        }

        public static bool IsByteExcludedOrNotPending(byte b)
        {
            return (b & 1 << 1) <= 0 || (b & 1) <= 0;
        }

    }
}
