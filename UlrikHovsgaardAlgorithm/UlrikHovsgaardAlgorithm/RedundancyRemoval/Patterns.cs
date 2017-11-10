﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Utils;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    /// <summary>
    /// Tools to execute patterns as well as pattern functions themselves.
    /// All of the patterns presently modify the given DcrGraphSimple.
    /// </summary>
    public static class Patterns
    {
        /// <summary>
        /// Maps from a pattern-name to the amount of redudancy-events discovered by it as an integer,
        /// and the time spent executing the pattern as a TimeSpan.
        /// </summary>
        public static Dictionary<string, RedundancyRemoverComparer.RedundancyStatistics> PatternStatistics = new Dictionary<string, RedundancyRemoverComparer.RedundancyStatistics>();

        public static void ResetStatistics() => PatternStatistics = new Dictionary<string, RedundancyRemoverComparer.RedundancyStatistics>();

        public static List<RedundancyEvent> ExecuteWithStatistics(Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>> func, DcrGraphSimple dcr, Activity act, int round)
        {
            if (!_measureRunningTimes)
                return func.Invoke(dcr, act, round);

            // Else: Perform running-time measurements and store them with the invoked method's name
            var start = DateTime.Now;
            var result = func.Invoke(dcr, act, round);
            var end = DateTime.Now;

            // Add the running time to the combined running time for this pattern-search method
            if (PatternStatistics.TryGetValue(func.Method.Name, out var stats))
            {
                stats.RedundancyCount += result.Count;
                stats.TimeSpent = stats.TimeSpent.Add(end - start);
            }
            else
                PatternStatistics.Add(func.Method.Name, new RedundancyRemoverComparer.RedundancyStatistics { RedundancyCount = result.Count, TimeSpent = end - start });
            //Console.WriteLine($"{func.Method.Name} took {end - start:g}");

            return result;
        }

        public static List<RedundancyEvent> ExecuteWithStatistics(Func<DcrGraphSimple, int, List<RedundancyEvent>> func, DcrGraphSimple dcr, int round)
        {
            if (!_measureRunningTimes)
                return func.Invoke(dcr, round);

            // Else: Perform running-time measurements and store them with the invoked method's name
            var start = DateTime.Now;
            var result = func.Invoke(dcr, round);
            var end = DateTime.Now;

            // Add the running time to the combined running time for this pattern-search method
            if (PatternStatistics.TryGetValue(func.Method.Name, out var stats))
            {
                stats.RedundancyCount += result.Count;
                stats.TimeSpent = stats.TimeSpent.Add(end - start);
            }
            else
                PatternStatistics.Add(func.Method.Name, new RedundancyRemoverComparer.RedundancyStatistics { RedundancyCount = result.Count, TimeSpent = end - start });
            //Console.WriteLine($"{func.Method.Name} took {end - start:g}");

            return result;
        }


        #region Pattern implementations

        /// <summary>
        /// A -->+ B(x)
        /// [C!] -->+ B
        /// [C!] -->* B
        /// ! -->% [C!]
        /// 
        /// A should not also have a condition to B!
        /// 
        /// </summary>
        public static List<RedundancyEvent> ApplyCondtionedInclusionPattern(DcrGraphSimple dcr, Activity A, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "ConditionedInclusionPattern";

            if (!A.Included) return events;

            // TODO: The attempted-to-discover pattern actually has to do with mutual exclusion, I think
            foreach (var B in new HashSet<Activity>(A.Includes(dcr)))
            {
                if (B.Included || A.HasResponseTo(B, dcr) || B.ExcludesMe(dcr).Count > 0 || hasChainConditionTo(A, B, dcr, new HashSet<Activity>())) continue;

                if (A.Id == "Collect Documents" && B.Id == "Make appraisal appointment")
                {
                    int i = 0;
                }
                foreach (var C in B.ConditionsMe(dcr))
                {
                    if (C.Included && C.Pending
                        && C.ExcludesMe(dcr).Count == 0
                        && C.Includes(dcr).Contains(B))
                    {
                        events.Add(new RedundantRelationEvent(patternName, RelationType.Inclusion, A, B, round));
                    }
                }
            }

            return events;
        }

        public static bool hasChainConditionTo(Activity from, Activity to, DcrGraphSimple dcr, HashSet<Activity> seenBefore)
        {
            if (seenBefore.Contains(from))
                return false;
            return from.HasConditionTo(to, dcr) || from.Conditions(dcr).Any(act =>
            {
                var newSet = new HashSet<Activity>(seenBefore);
                newSet.Add(from);
                return hasChainConditionTo(act, to, dcr, newSet);
            });
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// For graph G:
        /// A*--> [B!]
        /// , if B is never executable(meaning the initial Pending state is never removed).
        /// </summary>
        public static List<RedundancyEvent> ApplyRedundantResponsePattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "RedundantResponsePattern";

            // If pending and never executable --> Remove all incoming Responses
            if (act.Pending && !dcr.IsEverExecutable(act))
            {
                events.AddRange(act.ResponsesMe(dcr).Select(x =>
                    new RedundantRelationEvent(patternName, RelationType.Response, x, act, round)));
            }

            return events;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// Given two activities A and B, if A and B share their initial Included-status,
        /// and if forall C, C -->+ A, then C -->+ B,
        /// and if forall D, D -->% A, then D -->% B,
        /// then any inclusions between A and B are redundant.
        /// 
        /// This, overall, is because A and B are always Included or Excluded in unison, s.t. a subsequent
        /// Include between them would have no effect.
        /// </summary>
        public static List<RedundancyEvent> ApplyIncludesWhenAlwaysCommonlyExcludedAndIncludedPattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "IncludesWhenAlwaysCommonlyExcludedAndIncludedPattern";

            var excludesMe = new HashSet<Activity>(act.ExcludesMe(dcr));
            var includesMe = new HashSet<Activity>(act.IncludesMe(dcr));

            foreach (var other in act.Includes(dcr))
            {
                // Need to have the same initial Included state:
                if (act.Included != other.Included) continue;

                // Should share all incoming exclusions AND inclusions:
                if (excludesMe.Union(other.ExcludesMe(dcr)).Count() == excludesMe.Count
                    && includesMe.Union(other.IncludesMe(dcr)).Count() == includesMe.Count)
                {
                    // We already know that act -->+ other exists due to foreach
                    events.Add(new RedundantRelationEvent(patternName, RelationType.Inclusion, act, other, round));
                    // Conditionally also remove the other way around (Avoids dual evaluation from the perspective of 'other' later)
                    if (other.Includes(dcr).Contains(act))
                        events.Add(new RedundantRelationEvent(patternName, RelationType.Inclusion, other, act, round));
                }
            }

            return events;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// </summary>
        public static List<RedundancyEvent> ApplySequentialSingularExecutionLevelsPattern(DcrGraphSimple dcr, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "SequentialSingularExecutionLevelsPattern";

            var levels = dcr.GetSequentialSingularExecutionLevels();

            HashSet<Activity> previousLevelsActivities = new HashSet<Activity>();
            var allInLevels = new HashSet<Activity>(levels.SelectMany(x => x));
            var notInLevels = dcr.Activities.Where(x => !allInLevels.Contains(x));
            foreach (var level in levels)
            {
                foreach (var act in level)
                {
                    // Can remove all Conditions leaving the level (backwards and forwards (leaving level) are all redundant,
                    // and inter-level conditions are not allowed for a valid level.
                    events.AddRange(act.Conditions(dcr).Select(c => new RedundantRelationEvent(patternName, RelationType.Condition, act, c, round)));

                    // Can remove any outgoing Exclusion that does not target an activity in the same level's activities
                    events.AddRange(act.Excludes(dcr).Where(e => !level.Contains(e)).Select(e =>
                        new RedundantRelationEvent(patternName, RelationType.Exclusion, act, e, round)));

                    // Can remove any outgoing Response which targets an activity in a prior level
                    events.AddRange(act.Responses(dcr).Where(other => previousLevelsActivities.Contains(other))
                        .Select(other => new RedundantRelationEvent(patternName, RelationType.Response, act, other, round)));

                    // Can remove any forwards outgoing Response (leaving the level) which targets an initially Pending activity
                    events.AddRange(act.Responses(dcr).Where(other => !previousLevelsActivities.Contains(other) && !level.Contains(other) && other.Pending)
                        .Select(other => new RedundantRelationEvent(patternName, RelationType.Response, act, other, round)));

                    // Can remove any incoming relations from outside the levels' activities that aren't Includes (can't be any, since they'd then be part of a level)
                    // For Exclusions, we may not remove one such Exclusion if that exclusion was the reason that this activity got to be part of a level.
                    // ^--> This means that either we must self-exclude (in which case we can remove all future incoming excludes) or we mustn't be including that activity.
                    events.AddRange(act.ExcludesMe(dcr).Where(x => notInLevels.Contains(x) && (act.Excludes(dcr).Contains(act) || !act.Includes(dcr).Contains(x))).Select(other =>
                        new RedundantRelationEvent(patternName, RelationType.Exclusion, other, act, round)));
                    // Other relation-types can be removed regardless of whether or not they were part of the last fringe (level-search-attempt)
                    events.AddRange(act.ResponsesMe(dcr).Where(x => notInLevels.Contains(x)).Select(other =>
                        new RedundantRelationEvent(patternName, RelationType.Response, other, act, round)));
                    events.AddRange(act.ConditionsMe(dcr).Where(x => notInLevels.Contains(x)).Select(other =>
                        new RedundantRelationEvent(patternName, RelationType.Condition, other, act, round)));

                }

                previousLevelsActivities.UnionWith(level);
            }

            return events;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// For graph G:
        /// [A] -->+ B
        /// [A] -->* B
        /// , if [A] -->+ B is the only inclusion to B, then the condition is redundant.
        /// </summary>
        public static List<RedundancyEvent> ApplyRedundantPrecedencePattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "RedundantPrecedencePattern";

            if (!act.Included // This act is excluded
                              // A single ingoing Inclusion
                && act.IncludesMe(dcr).Count == 1
                // That same activity also has condition to us
                && act.ConditionsMe(dcr).Contains(act.IncludesMe(dcr).First()))
            {
                // The condition is redundant since we can only be included by the conditioner
                events.Add(new RedundantRelationEvent(patternName, RelationType.Condition, act.IncludesMe(dcr).First(), act, round));
            }

            return events;
        }

        /// <summary>
        /// ORIGIN: Discovered 15th of September following study of 2 redundant conditions in graph mined from Mortgage-graph-generated log.
        /// 
        /// For graph G:
        /// [A] -->* [B] -->* C
        /// [A] -->* C
        /// , if there are no ingoing Exclusions to B,
        /// and A is not both included and excluded from outside sources, (since if first A excluded, B executed,
        /// and then A included again, then [A] -->* C would have effect)
        /// then [A] -->* C is redundant, because C is still bound by [B] -->* C.
        /// </summary>
        public static List<RedundancyEvent> ApplyRedundantTransitiveConditionWith3ActivitiesPattern(DcrGraphSimple dcr, Activity C, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "RedundantTransitiveConditionWith3ActivitiesPattern";

            // At least two incoming conditions to C (A and B)
            if (C.ConditionsMe(dcr).Count < 2)
                return events;

            var incomingConditionsC_Copy = new HashSet<Activity>(C.ConditionsMe(dcr));

            foreach (var B in incomingConditionsC_Copy)
            {
                // No incoming exclusions to B
                if (B.ExcludesMe(dcr).Count > 0)
                    continue;

                foreach (var A in incomingConditionsC_Copy)
                {
                    if (A.Equals(B)) continue; // Another incoming condition to C (therefore not B)

                    // A must not be both excludable and includable from activities other than B and C
                    if (A.IncludesMe(dcr).Except(new List<Activity> { B, C }).Any()
                        && A.ExcludesMe(dcr).Except(new List<Activity> { B, C }).Any())
                        continue;

                    if (// [A] -->* [B]
                        A.Included
                        && A.Conditions(dcr).Contains(B) &&
                            (B.Included ||
                            // If not included, A must include B
                            A.Includes(dcr).Contains(B)) && !C.IncludesMe(dcr).Any(x => x != A)
                        )
                    {
                        // The condition is redundant since we can only be included by the conditioner
                        events.Add(new RedundantRelationEvent(patternName, RelationType.Condition, A, C, round));
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// ORIGIN: Expanded on 15th of September to enforce all ingoing B-inclusions to also have to include C, in order for B -->+ C to be considered correctly redundant.
        /// - This change was discovered on  due to lack of detection of redundant inclusion seemingly following the old version of the pattern.
        /// 
        /// For graph G: [Example with inclusion to Excluded activity dependency]
        /// When:
        ///   B is excluded /\ exists C,
        ///    (Forall A, A -->+ B => A -->+ C) /\ (Forall D, !D -->% C),
        ///   then B -->+ C is redundant
        /// </summary>
        public static List<RedundancyEvent> ApplyRedundantChainInclusionPattern(DcrGraphSimple dcr, Activity B, int round)
        {
            var events = new List<RedundancyEvent>();

            var patternName = "RedundantChainInclusionPattern";
            if (B.Included) return events; // B must be excluded

            // Iterate all ingoing inclusions to B - they must all also include C, in order for B -->+ C to be redundant
            if (B.Includes(dcr).Count == 0 || B.IncludesMe(dcr).Count == 0)
                return events;

            // TODO: May avoid considering ingoing Inclusions to B from activities that are not executable at start-time.
            // TODO: - This would capture >=1 more case in mortgage graph, since such an includer shouldn't be required to
            // TODO:   also include C.

            foreach (var C in new HashSet<Activity>(B.Includes(dcr))) // Since we might remove inclusions, we cannot foreach the collection directly
            {
                // Skip if C is excludable
                if (dcr.ExcludesInverted.TryGetValue(C, out var exclSourcesC)
                    && exclSourcesC.Count > 0)
                    continue;

                var canDo = true;
                foreach (var inclSourceB in B.IncludesMe(dcr))
                {
                    canDo = canDo && (C.IncludesMe(dcr).Contains(inclSourceB) ||
                        Equals(inclSourceB, C) || Chase(dcr, inclSourceB, C.IncludesMe(dcr), 4));
                }
                if (canDo)
                {
                    events.Add(new RedundantRelationEvent(patternName, RelationType.Inclusion, B, C, round));
                }

            }

            return events;
        }

        public static Boolean Chase(DcrGraphSimple dcr, Activity A, HashSet<Activity> mustInclude, int countdown)
        {
            if (countdown == 0)
                return false;
            var isFine = true;
            foreach (var incoming in A.IncludesMe(dcr))
            {
                isFine = isFine && (mustInclude.Contains(incoming) ||
                                    Chase(dcr, incoming, mustInclude, countdown - 1));
            }
            return isFine;
        }

        public static List<RedundancyEvent> ApplyRedundantChainResponsePattern(DcrGraphSimple dcr, Activity C, int round)
        {
            var events = new List<RedundancyEvent>();
            var pattern = "RedundantChainResponsePattern";

            foreach (var A in C.ResponsesMe(dcr))
            {
                if (ResponseChase(dcr, null, A, C, 4))
                {
                    events.Add(new RedundantRelationEvent(pattern, RelationType.Response, A, C, round));
                }
            }

            return events;
        }

        public static Boolean ResponseChase(DcrGraphSimple dcr, Activity previous, Activity current, Activity target, int countdown)
        {
            var hello = "";
            if (current.Id.Contains("Budget screening"))
            {
                var h = current.HasResponseTo(target, dcr);
                var hh = current.ExcludesMe(dcr);
                var hhh = previous.HasIncludeTo(current, dcr);
                var hhhhh = previous.Includes(dcr);
                var hhhh = current.Responses(dcr);
                var blah = "";
            }
            if (countdown == 0)
                return false;
            if (previous != null && current.HasResponseTo(target, dcr) && current.ExcludesMe(dcr).Count == 0 && (current.Included || (previous != null && previous.HasIncludeTo(current, dcr))))
            {
                return true;
            }
            return current.Responses(dcr).Any(other => ResponseChase(dcr, current, other, target, countdown - 1));
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// If an activity can never be executed, all of its after-execution relations have no effect,
        /// and can thus be removed.
        /// </summary>
        public static List<RedundancyEvent> ApplyRedundantRelationsFromUnExecutableActivityPattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var patternName = "RedundantRelationsFromUnExecutableActivityPattern";
            var events = new List<RedundancyEvent>();

            if (!dcr.IsEverExecutable(act))
            {
                // Register all events of relations about to be removed (all outgoing relations)
                events.AddRange(act.Includes(dcr)
                    .Select(x => new RedundantRelationEvent(patternName, RelationType.Inclusion, act, x, round)));
                events.AddRange(act.ExcludesMe(dcr).Select(x =>
                    new RedundantRelationEvent(patternName, RelationType.Exclusion, x, act, round)));
                events.AddRange(act.Responses(dcr).Select(x =>
                    new RedundantRelationEvent(patternName, RelationType.Response, act, x, round)));

                // Note: Conditions still have effect
            }

            return events;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// TODO: Can probably be generalized
        /// </summary>
        public static List<RedundancyEvent> ApplyLastConditionHoldsPattern(DcrGraphSimple dcr, Activity A, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "LastConditionHoldsPattern";

            foreach (var B in dcr.Activities)
            {
                if (A.Included && B.Included
                    // A excludes B
                    && A.Excludes(dcr).Contains((B))
                    // Nobody excludes A
                    && dcr.NobodyExcludes(A)
                    // Nobody includes B (meaning excluded forever after A executes)
                    && dcr.NobodyIncludes(B))
                {
                    // ... and they share an outgoing Condition target
                    foreach (var intersectedActivity in A.Conditions(dcr).Intersect(B.Conditions(dcr)))
                    {
                        events.Add(new RedundantRelationEvent(patternName, RelationType.Condition, B, intersectedActivity, round));
                    }
                }
            }

            return events;
        }

        // TODO: Unused pattern - work on it?
        public static List<RedundancyEvent> ApplyRedundantIncludeWhenIncludeConditionExistsPattern(DcrGraphSimple dcr, Activity A, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "RedundantIncludeWhenIncludeConditionExists";

            foreach (var B in A.Includes(dcr))
            {
                foreach (var C in B.IncludesMe(dcr))
                {
                    if (A.Equals(C))
                        continue;
                    if (C.HasConditionTo(B, dcr) && C.ExcludesMe(dcr).Count == 0)
                    {
                        events.Add(new RedundantRelationEvent(patternName, RelationType.Inclusion, A, B, round));
                        dcr.RemoveInclude(A, B);
                    }
                }
            }
            return events;
        }

        #endregion

        #region Basic RR-logic (Perhaps too "simple" for pattern-definition)

        public static List<RedundancyEvent> ApplyBasicRedundancyRemovalLogic(DcrGraphSimple dcr, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "BasicRedundancyRemovalLogic";

            // Remove everything that is excluded and never included
            foreach (var act in dcr.Activities.ToArray())
            {
                // If excluded and never included
                if (!act.Included && act.IncludesMe(dcr).Count == 0)
                {
                    // Remove activity and all of its relations
                    events.AddRange(act.Includes(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Inclusion, act, x, round)));
                    events.AddRange(act.IncludesMe(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Inclusion, x, act, round)));
                    events.AddRange(act.Excludes(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Exclusion, act, x, round)));
                    events.AddRange(act.ExcludesMe(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Exclusion, x, act, round)));
                    events.AddRange(act.Conditions(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Condition, act, x, round)));
                    events.AddRange(act.ConditionsMe(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Condition, x, act, round)));
                    events.AddRange(act.Responses(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Response, act, x, round)));
                    events.AddRange(act.ResponsesMe(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Response, x, act, round)));
                    events.Add(new RedundantActivityEvent(patternName, act));
                    dcr.MakeActivityDisappear(act);
                    //Console.WriteLine($"Excluded activity rule: Removed {res.Removed.Count - before} relations in total");
                }
                // If included and never excluded --> remove all incoming includes
                else if (act.Included && act.ExcludesMe(dcr).Count == 0)
                {
                    // Remove all incoming includes
                    events.AddRange(act.IncludesMe(dcr).Select(x =>
                        new RedundantRelationEvent(patternName, RelationType.Inclusion, x, act, round)));
                    dcr.RemoveAllIncomingIncludes(act);
                    //Console.WriteLine($"Always Included activity rule: Removed {res.Removed.Count - before} relations in total");
                }
            }

            return events;
        }

        #endregion
    }
}