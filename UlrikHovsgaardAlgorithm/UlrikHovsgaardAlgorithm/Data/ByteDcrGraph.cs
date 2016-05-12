using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UlrikHovsgaardAlgorithm.Data
{
    /// <summary>
    /// A compressed DcrGraph implementation, that can only run activities, and which cannot be dynamically built
    /// </summary>
    public class ByteDcrGraph
    {
        public byte[] State { get; } 
        public Dictionary<int, HashSet<int>> Includes { get; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> Excludes { get; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> Responses { get; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> ConditionsReversed { get; } = new Dictionary<int, HashSet<int>>();
        public Dictionary<int, HashSet<int>> MilestonesReversed { get; } = new Dictionary<int, HashSet<int>>();


        public ByteDcrGraph(DcrGraph inputGraph)
        {
            State = DcrGraph.HashDcrGraph(inputGraph);

            var activityList = inputGraph.GetActivities().ToList();

            foreach (var inclExcl in inputGraph.IncludeExcludes)
            {
                var source = activityList.FindIndex(a => a.Equals(inclExcl.Key));
                foreach (var keyValuePair in inclExcl.Value)
                {
                    var target = activityList.FindIndex(a => a.Equals(keyValuePair.Key));
                    if (keyValuePair.Value)
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
                    else
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
                var source = activityList.FindIndex(a => a.Equals(response.Key));
                foreach (var target in response.Value)
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
                foreach (var target in condition.Value)
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
                foreach (var target in milestone.Value)
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
            State = new byte[byteDcrGraph.State.Count()];
            byteDcrGraph.State.CopyTo(State, 0);

            Includes = byteDcrGraph.Includes;
            Excludes = byteDcrGraph.Excludes;
            Responses = byteDcrGraph.Responses;
            ConditionsReversed = byteDcrGraph.ConditionsReversed;
            MilestonesReversed = byteDcrGraph.MilestonesReversed;
        }

        public List<int> GetRunnableIndexes()
        {
            var resList = new List<int>();

            for (int i = 0; i < State.Count(); i++)
            {
                if (ActivityCanRun(i))
                {
                    resList.Add(i);
                }
            }
            
            return resList;
        }

        private bool ActivityCanRun(int idx)
        {
            if (!IsByteIncluded(State[idx])) return false;
            if (ConditionsReversed.ContainsKey(idx))
            {
                if (ConditionsReversed[idx].Any(source => !IsByteExcludedOrExecuted(State[source])))
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
                int i = 0;
            }
        }

        private void SetActivityPending(int idx)
        {
            // Pending = true
            State[idx] = (byte)((State[idx]) | 1);
            int i = 0;
        }

        public static bool IsByteIncluded(byte b)
        {
            return (b & 1 << 1) > 0;
        }

        public static bool IsByteExcludedOrExecuted(byte b)
        {
            return (b & 1 << 1) <= 0 || (b & 1 << 2) > 0;
        }

        public static bool IsByteExcludedOrNotPending(byte b)
        {
            return (b & 1 << 1) <= 0 || (b & 1) <= 0;
        }

        public ByteDcrGraph Copy() // Maybe just another constructor...
        {
            return this;
        }


        //public int hashCode()
        //{
        //    int sum = 0;
        //    for (int r = 0; r < bytes.length; r++) {
        //        sum = (b[r]) + sum * 11;
        //    }
        //    return sum;
        //}
    }
}
