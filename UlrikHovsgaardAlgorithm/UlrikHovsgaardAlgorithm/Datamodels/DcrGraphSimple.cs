﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithm.Datamodels
{
    public class DcrGraphSimple
    {
        #region Initialization

        public DcrGraphSimple(HashSet<Activity> activities)
        {
            Activities = activities;
        }

        #endregion

        #region Properties

        public string Title { get; set; }
        public HashSet<Activity> Activities { get; set; } = new HashSet<Activity>();
        public Dictionary<Activity, HashSet<Activity>> Responses { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Includes { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Excludes { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Conditions { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();
        //public Dictionary<Activity, HashSet<Activity>> Milestones { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();

        public Dictionary<Activity, HashSet<Activity>> ResponsesInverted { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> IncludesInverted { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> ExcludesInverted { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> ConditionsInverted { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();
        //public Dictionary<Activity, HashSet<Activity>> MilestonesRev { get; private set; } = new Dictionary<Activity, HashSet<Activity>>();


        public int RelationsCount => IncludesCount + ExcludesCount + ResponsesCount + ConditionsCount;
        public int IncludesCount => Includes.Values.Sum(x => x.Count);
        public int ExcludesCount => Excludes.Values.Sum(x => x.Count);
        public int ResponsesCount => Responses.Values.Sum(x => x.Count);
        public int ConditionsCount => Conditions.Values.Sum(x => x.Count);

        #endregion

        #region Methods

        public bool SanityCheck()
        {
            return RelationsCount == IncludesInverted.Values.Sum(x => x.Count) + ExcludesInverted.Values.Sum(x => x.Count) +
                   ResponsesInverted.Values.Sum(x => x.Count) + ConditionsInverted.Values.Sum(x => x.Count);
        }

        public void MakeActivityDisappear(Activity act)
        {
            if (act.Id == "Make appraisal appointment" || act.Name == "Make appraisal appointment")
            {
                var bla = 0;
                bla++;
            }

            // Remove relations
            RemoveAllOccurrences(act, Includes, IncludesInverted);
            RemoveAllOccurrences(act, Excludes, ExcludesInverted);
            RemoveAllOccurrences(act, Responses, ResponsesInverted);
            RemoveAllOccurrences(act, Conditions, ConditionsInverted);

            Activities.Remove(act);
        }

        public void AddInclude(Activity sourceId, Activity targetId)
        {
            AddActivitiesToRelationDictionary(sourceId, targetId, Includes, IncludesInverted);
        }

        public void AddExclude(Activity sourceId, Activity targetId)
        {
            AddActivitiesToRelationDictionary(sourceId, targetId, Excludes, ExcludesInverted);
        }

        public void AddResponse(Activity sourceId, Activity targetId)
        {
            AddActivitiesToRelationDictionary(sourceId, targetId, Responses, ResponsesInverted);
        }

        public void AddCondition(Activity sourceId, Activity targetId)
        {
            AddActivitiesToRelationDictionary(sourceId, targetId, Conditions, ConditionsInverted);
        }

        public void RemoveInclude(Activity sourceId, Activity targetId)
        {
            RemoveRelation(sourceId, targetId, Includes, IncludesInverted);
        }

        public void RemoveExclude(Activity sourceId, Activity targetId)
        {
            RemoveRelation(sourceId, targetId, Excludes, ExcludesInverted);
        }

        public void RemoveResponse(Activity sourceId, Activity targetId)
        {
            RemoveRelation(sourceId, targetId, Responses, ResponsesInverted);
        }

        public void RemoveCondition(Activity source, Activity target)
        {
            RemoveRelation(source, target, Conditions, ConditionsInverted);
        }

        private HashSet<Activity> _dependsOnVisited;
        public bool DependsOnOnly(Activity act, Activity dependsOnAct)
        {
            _dependsOnVisited = new HashSet<Activity>();
            return DependsOnOnlyInner(act, dependsOnAct);
        }

        private bool DependsOnOnlyInner(Activity act, Activity dependsOnAct)
        {
            // Excluded and only included by dependant
            var onlyIncludedBy = !act.Included &&
                                 IncludesInverted.TryGetValue(act, out var includeSources) &&
                                 includeSources.Count == 1 && includeSources.Contains(dependsOnAct);

            // Condition and source never excluded
            var singularlyConditionallyBound =
                // Source never excluded:
                (!ExcludesInverted.TryGetValue(dependsOnAct, out var dependantExcludeSources) ||
                 dependantExcludeSources.Count == 0)
                 &&
                // DependsOn has singular condition to act:
                IncludesInverted.TryGetValue(act, out var conditionSources) &&
                conditionSources.Count == 1 && conditionSources.Contains(dependsOnAct);

            // TODO: Chain dependency ??? - depends on usage purpose

            return onlyIncludedBy || singularlyConditionallyBound;
        }

        /// <summary>
        /// Considers a simple version of the DCR-graph where only Excluded states and
        /// Include relations are considered. Then figures out where activities are placed in relation to each other,
        /// based on when they may (without any other constraints) first be included, namely grouping
        /// activities by the minimum number of steps required to reach them, based on the Inclusion-hierarchy in the graph.
        /// 
        /// Returns a list of levels.
        /// </summary>
        public List<HashSet<Activity>> GetSequentialSingularExecutionLevels()
        {
            var levels = new List<HashSet<Activity>>();
            HashSet<Activity> tagged;

            // Set up first level:
            var initiallyIncluded = new HashSet<Activity>(Activities.Where(a => a.Included));
            if (IsLevelValid(new HashSet<Activity>(), initiallyIncluded))
            {
                levels.Add(initiallyIncluded); // First level is the initially Included activities
                tagged = new HashSet<Activity>(initiallyIncluded);
            }
            else
            {
                return new List<HashSet<Activity>>();
            }


            var lastLevelIndex = 0; // Index-access of the last level

            while (tagged.Count < this.Activities.Count)
            {
                var newReachables = levels[lastLevelIndex].SelectMany(a => a.Includes(this)).Except(tagged).ToList();

                if (newReachables.Count == 0)
                {
                    // Something is unreachable. We are finished
                    return levels;
                }

                // Check the newly discovered level, and only add it if it is a valid level
                if (IsLevelValid(levels[lastLevelIndex], new HashSet<Activity>(newReachables)))
                {
                    levels.Add(new HashSet<Activity>(newReachables)); // Next level is the newly "included" activities
                    tagged = new HashSet<Activity>(tagged.Union(newReachables));
                }
                else return levels;
                
                lastLevelIndex++;
            }

            return levels;
        }

        public bool IsLevelValid(HashSet<Activity> previousLevel, HashSet<Activity> activities)
        {
            foreach (var act in activities)
            {
                var includesMe = act.IncludesMe(this);

                // At most one activity may include this activity
                if (includesMe.Count > 1)
                    return false;

                // All the level's activities must not be included by anyone outside the previous layers' activities
                // ^--> we know they will be Excluded and never again Included by now
                if (!includesMe.All(previousLevel.Contains))
                    return false;
                
                // Also, don't have any conditions between activities in the same level! TODO: Why do we have this constraint?
                if (act.Conditions(this).Any(activities.Contains) || act.ConditionsMe(this).Any(activities.Contains))
                    return false;

                // Either we exclude ourselves
                if (act.Excludes(this).Contains(act))
                    continue;

                // Else: Everyone we include must exclude us, to ensure we are excluded in future layers
                var includes = act.Includes(this);
                foreach (var includer in includes)
                {
                    if (!includer.Excludes(this).Contains(act))
                        return false;
                }
            }

            return true;
        }
        
        public bool NobodyIncludes(Activity act)
        {
            return (!IncludesInverted.TryGetValue(act, out var inclSources) || inclSources.Count == 0);
        }

        public bool NobodyExcludes(Activity act)
        {
            return (!ExcludesInverted.TryGetValue(act, out var exclSources) || exclSources.Count == 0);
        }

        public bool NobodyResponses(Activity act)
        {
            return (!ResponsesInverted.TryGetValue(act, out var respSources) || respSources.Count == 0);
        }

        public bool NobodyConditions(Activity act)
        {
            return (!ConditionsInverted.TryGetValue(act, out var condSources) || condSources.Count == 0);
        }

        public int RemoveAllIncomingIncludes(Activity act)
        {
            return RemoveAllIncoming(act, Includes, IncludesInverted);
        }

        public int RemoveAllIncomingExcludes(Activity act)
        {
            return RemoveAllIncoming(act, Excludes, ExcludesInverted);
        }

        public int RemoveAllIncomingResponses(Activity act)
        {
            return RemoveAllIncoming(act, Responses, ResponsesInverted);
        }

        public int RemoveAllIncomingConditions(Activity act)
        {
            return RemoveAllIncoming(act, Conditions, ConditionsInverted);
        }

        public int RemoveAllOutgoingIncludes(Activity act)
        {
            return RemoveAllOutgoing(act, Includes, IncludesInverted);
        }

        public int RemoveAllOutgoingExcludes(Activity act)
        {
            return RemoveAllOutgoing(act, Excludes, ExcludesInverted);
        }

        public int RemoveAllOutgoingResponses(Activity act)
        {
            return RemoveAllOutgoing(act, Responses, ResponsesInverted);
        }

        public int RemoveAllOutgoingConditions(Activity act)
        {
            return RemoveAllOutgoing(act, Conditions, ConditionsInverted);
        }

        #region Private methods

        private void AddActivitiesToRelationDictionary(
            Activity sourceAct,
            Activity targetAct,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            // Forward
            HashSet<Activity> targets;
            if (dict.TryGetValue(sourceAct, out targets))
                targets.Add(targetAct);
            else
                dict.Add(sourceAct, new HashSet<Activity> { targetAct });

            // Inverted
            HashSet<Activity> sources;
            if (dictInv.TryGetValue(targetAct, out sources))
                sources.Add(sourceAct);
            else
                dictInv.Add(targetAct, new HashSet<Activity> { sourceAct });
        }

        private void RemoveRelation(
            Activity sourceAct,
            Activity targetAct,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            // Forward
            if (dict.TryGetValue(sourceAct, out var targets))
            {
                targets.Remove(targetAct);
                if (targets.Count == 0) // If last outgoing relation
                    dict.Remove(sourceAct);
            }

            // Inverted
            if (dictInv.TryGetValue(targetAct, out var sources))
            {
                sources.Remove(sourceAct);
                if (sources.Count == 0) // If last incoming relation
                    dictInv.Remove(targetAct);
            }
        }

        private void RemoveAllOccurrences(
            Activity act,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            RemoveAllIncoming(act, dict, dictInv);
            RemoveAllOutgoing(act, dict, dictInv);
        }

        private int RemoveAllOutgoing
            (Activity act,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            // First count amount of outgoing relations about to be removed
            var removedRelations = dict.ContainsKey(act) ? dict[act].Count : 0;

            // Remove any occurence of act as a source to any target in the inverse dictionary:
            foreach (var kvPair in dictInv.ToDictionary(x => x.Key, x => x.Value)) // Clone
            {
                kvPair.Value.Remove(act); // Remove this activity as a source targeting other activities in inverted dictionary
                if (kvPair.Value.Count == 0)
                    dictInv.Remove(kvPair.Key);
            }
                
            dict.Remove(act);

            return removedRelations;
        }

        private int RemoveAllIncoming(
            Activity act,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            var removedRelations = 0;

            if (dictInv.TryGetValue(act, out var sources))
            {
                foreach (var sourceAct in sources)
                {
                    // Remove from outgoing dictionaries of other activities:
                    var targets = dict[sourceAct];
                    if (targets.Remove(act))
                    {
                        removedRelations++;
                    }

                    // Clean-up:
                    if (targets.Count == 0)
                        dict.Remove(sourceAct);
                }
            }

            // No longer any targets pointing to act (removed above)
            dictInv.Remove(act);

            return removedRelations;
        }

        #endregion

        public List<Activity> GetRunnableActivities()
        {
            return Activities.Where(x => x.Included && x.ConditionsMe(this).All(y => !x.Included || x.Executed)).ToList();
        }

        public static byte[] HashDcrGraph(DcrGraphSimple graph)
        {
            var array = new byte[graph.Activities.Count];
            int i = 0;
            var runnables = graph.GetRunnableActivities();
            foreach (var act in graph.Activities.OrderBy(x => x.Id))
            {
                array[i++] = act.HashActivity(runnables.Contains(act));
            }
            return array;
        }

        /// <summary>
        /// Creates a clone of this DcrGraphSimple, which should be equal when comparing the two afterwards.
        /// </summary>
        public DcrGraphSimple Copy()
        {
            return new DcrGraphSimple(new HashSet<Activity>(Activities))
            {
                Title = Title,
                // Clone all collections (to ensure modifications affect only one DcrGraphSimple instance):
                Includes = Includes.ToDictionary(x => x.Key, x => new HashSet<Activity>(x.Value)),
                Excludes = Excludes.ToDictionary(x => x.Key, x => new HashSet<Activity>(x.Value)),
                Conditions = Conditions.ToDictionary(x => x.Key, x => new HashSet<Activity>(x.Value)),
                Responses = Responses.ToDictionary(x => x.Key, x => new HashSet<Activity>(x.Value)),
                IncludesInverted = IncludesInverted.ToDictionary(x => x.Key, x => new HashSet<Activity>(x.Value)),
                ExcludesInverted = ExcludesInverted.ToDictionary(x => x.Key, x => new HashSet<Activity>(x.Value)),
                ConditionsInverted = ConditionsInverted.ToDictionary(x => x.Key, x => new HashSet<Activity>(x.Value)),
                ResponsesInverted = ResponsesInverted.ToDictionary(x => x.Key, x => new HashSet<Activity>(x.Value))
            };
        }

        /// <summary>
        /// Checks for existance of all activities and all outgoing relation-dictionaries.
        /// 
        /// Works only for DcrGraphSimple instances that have been built using the methods provided
        /// by the class itself. (Unsafe)
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = (DcrGraphSimple) obj;
            if (other == null) return false;
            
            // Check that all outgoing dictionaries have the same size (avoid under-shooting the comparison with "other")
            if (IncludesCount != other.IncludesCount
                || ExcludesCount != other.ExcludesCount
                || ConditionsCount != other.ConditionsCount
                || ResponsesCount != other.ResponsesCount
                || RelationsCount != other.RelationsCount)
                return false;

            foreach (var act in Activities)
            {
                var otherAct = other.Activities.FirstOrDefault(x => x.Id == act.Id);
                if (otherAct == null)
                    return false;

                if (Includes.TryGetValue(act, out var inclusions))
                {
                    foreach (var target in inclusions)
                    {
                        if (!other.Includes.TryGetValue(otherAct, out var otherInclusions)
                            || !otherInclusions.Any(x => x.Id == target.Id))
                            return false;
                    }
                }

                if (Excludes.TryGetValue(act, out var exclusions))
                {
                    foreach (var target in exclusions)
                    {
                        if (!other.Excludes.TryGetValue(otherAct, out var otherExclusions)
                            || !otherExclusions.Any(x => x.Id == target.Id))
                            return false;
                    }
                }

                if (Conditions.TryGetValue(act, out var conditions))
                {
                    foreach (var target in conditions)
                    {
                        if (!other.Conditions.TryGetValue(otherAct, out var otherConditions)
                            || !otherConditions.Any(x => x.Id == target.Id))
                            return false;
                    }
                }

                if (Responses.TryGetValue(act, out var responses))
                {
                    foreach (var target in responses)
                    {
                        if (!other.Responses.TryGetValue(otherAct, out var otherResponses)
                            || !otherResponses.Any(x => x.Id == target.Id))
                            return false;
                    }
                }
            }

            return true;
        }

        public DcrGraph ToDcrGraph()
        {
          
            var graph = new DcrGraph();
            foreach (var act in Activities)
            {
                var newAct = graph.AddActivity(act.Id, act.Name);
                newAct.Included = act.Included;
                newAct.Executed = act.Executed;
                newAct.Pending = act.Pending;

            }
            foreach (var pair in Includes)
            {
                foreach (var target in pair.Value)
                {
                    graph.AddIncludeExclude(true, pair.Key.Id, target.Id);
                }
            }
            foreach (var pair in Excludes)
            {
                foreach (var target in pair.Value)
                {
                    graph.AddIncludeExclude(false, pair.Key.Id, target.Id);
                }
            }
            foreach (var pair in Conditions)
            {
                foreach (var target in pair.Value)
                {
                    graph.AddCondition(pair.Key.Id, target.Id);
                }
            }
            foreach (var pair in Responses)
            {
                foreach (var target in pair.Value)
                {
                    graph.AddResponse(pair.Key.Id, target.Id);
                }
            }
            return graph;
        }
        #endregion
    }
}
