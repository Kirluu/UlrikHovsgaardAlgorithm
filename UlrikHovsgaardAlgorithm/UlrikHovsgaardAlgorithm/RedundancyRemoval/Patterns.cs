using System;
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
        /// Applies all our great patterns.
        /// </summary>
        /// <returns>Amount of relations removed</returns>
        public static (Dictionary<string, (List<RedundancyEvent>, TimeSpan)>, List<RedundancyEvent>) ApplyPatterns(
            DcrGraphSimple dcr,
            int iterations,
            HashSet<Func<DcrGraphSimple, int, List<RedundancyEvent>>> graphwidePatterns,
            HashSet<Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>>> activityPatterns)
        {
            var statisticsPerPattern = new Dictionary<string, (List<RedundancyEvent>, TimeSpan)>(); // Storing relations found and time spent
            var actualEventList = new List<RedundancyEvent>();

            foreach (var pattern in graphwidePatterns)
            {
                var (evs, timeSpent) = ExecuteAndMeasure(pattern, dcr, iterations);
                statisticsPerPattern.AddOrUpdate(pattern.Method.Name, (evs, timeSpent));
                actualEventList.AddRange(evs);

                RedundancyRemoverComparer.ApplyEventsOnGraph(dcr, evs);
            }

            foreach (var act in dcr.Activities)
            {
                foreach (var pattern in activityPatterns)
                {
                    var (evs, timeSpent) = ExecuteAndMeasure(pattern, dcr, act, iterations);
                    statisticsPerPattern.AddOrUpdate(pattern.Method.Name, (evs, timeSpent));
                    actualEventList.AddRange(evs);

                    RedundancyRemoverComparer.ApplyEventsOnGraph(dcr, evs);
                }
            }

            return (statisticsPerPattern, actualEventList);
        }

        public static (List<RedundancyEvent>, TimeSpan) ExecuteAndMeasure(
            Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>> func,
            DcrGraphSimple dcr,
            Activity act,
            int round)
        {
            // Perform running-time measurements and store them with the invoked method's name
            var start = DateTime.Now;
            var result = func.Invoke(dcr, act, round);
            var end = DateTime.Now;

            // Add the running time to the combined running time for this pattern-search method
            //if (PatternStatistics.TryGetValue(func.Method.Name, out var stats))
            //{
            //    stats.RedundancyCount += result.Count;
            //    stats.TimeSpent = stats.TimeSpent.Add(end - start);
            //}
            //else
            //    PatternStatistics.Add(func.Method.Name, new RedundancyRemoverComparer.RedundancyStatistics { RedundancyCount = result.Count, TimeSpent = end - start });
            
            return (result, end - start);
        }

        public static (List<RedundancyEvent>, TimeSpan) ExecuteAndMeasure(
            Func<DcrGraphSimple, int, List<RedundancyEvent>> func,
            DcrGraphSimple dcr,
            int round)
        {
            // Perform execution-time measurements and store them with the invoked method's name
            var start = DateTime.Now;
            var result = func.Invoke(dcr, round);
            var end = DateTime.Now;

            // Add the running time to the combined running time for this pattern-search method
            //if (PatternStatistics.TryGetValue(func.Method.Name, out var stats))
            //{
            //    stats.RedundancyCount += result.Count;
            //    stats.TimeSpent = stats.TimeSpent.Add(end - start);
            //}
            //else
            //    PatternStatistics.Add(func.Method.Name, new RedundancyRemoverComparer.RedundancyStatistics { RedundancyCount = result.Count, TimeSpent = end - start });
            
            return (result, end - start);
        }

        /// <summary>
        /// Takes a redundancy event and applies it onto the given DCR graph, and also adds
        /// the event to the given event-collection.
        /// </summary>
        public static void ApplyAndAdd(DcrGraphSimple graph, List<RedundancyEvent> allEvents, IEnumerable<RedundancyEvent> eventsToApply)
        {
            foreach (var ev in eventsToApply.ToList()) // ToList required, since source of IEnumerable is probably about to be modified @ apply-step.
            {
                ApplyAndAdd(graph, allEvents, ev);
            }
        }

        /// <summary>
        /// Takes a redundancy event and applies it onto the given DCR graph, and also adds
        /// the event to the given event-collection.
        /// </summary>
        public static void ApplyAndAdd(DcrGraphSimple graph, List<RedundancyEvent> allEvents, IEnumerable<RedundantRelationEvent> eventsToApply)
        {
            foreach (var ev in eventsToApply.ToList()) // ToList required, since source of IEnumerable is probably about to be modified @ apply-step.
            {
                ApplyAndAdd(graph, allEvents, ev);
            }
        }

        /// <summary>
        /// Takes a redundancy event and applies it onto the given DCR graph, and also adds
        /// the event to the given event-collection.
        /// </summary>
        public static void ApplyAndAdd(DcrGraphSimple graph, List<RedundancyEvent> allEvents, RedundancyEvent eventToApply)
        {
            RedundancyRemoverComparer.ApplyEventOnGraph(graph, eventToApply);
            allEvents.Add(eventToApply);
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
            
            foreach (var B in new HashSet<Activity>(A.Includes(dcr)))
            {
                if (B.Included || A.HasResponseTo(B, dcr) || B.ExcludesMe(dcr).Count > 0 || hasChainConditionTo(A, B, dcr, new HashSet<Activity>())) continue;
                
                foreach (var C in B.ConditionsMe(dcr))
                {
                    // Any condtions from B should also be targeted by C, for pattern to be valid
                    if (!B.Conditions(dcr).All(c => C.HasConditionTo(c, dcr)))
                        continue;

                    if (C.Included && C.Pending
                        && C.ExcludesMe(dcr).Count == 0
                        && C.HasIncludeTo(B, dcr))
                    {
                        ApplyAndAdd(dcr, events, new RedundantRelationEvent(patternName, RelationType.Inclusion, A, B, round));
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
                ApplyAndAdd(dcr, events, act.ResponsesMe(dcr).Select(x =>
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

            foreach (var other in new HashSet<Activity>(act.Includes(dcr)))
            {
                // Need to have the same initial Included state:
                if (act.Included != other.Included) continue;

                var excludesOther = new HashSet<Activity>(other.ExcludesMe(dcr).Where(x => !Equals(x, act)));
                var includesOther = new HashSet<Activity>(other.IncludesMe(dcr).Where(x => !Equals(x, act)));
                var includesAct = new HashSet<Activity>(includesMe.Where(x => !Equals(x, other)));
                var excludesAct = new HashSet<Activity>(excludesMe.Where(x => !Equals(x, other)));
                if (excludesAct.Count == 0 || includesAct.Count == 0 || excludesOther.Count == 0 || includesOther.Count == 0)
                    continue;
                // Should share all incoming exclusions AND inclusions:
                if (excludesAct.SetEquals(excludesOther) && includesAct.SetEquals(includesOther))
                    
                    
                    //excludesMe.Intersect(other.ExcludesMe(dcr)).Count() == excludesMe.Count
                    //&& includesMe.Intersect(other.IncludesMe(dcr)).Count() == includesMe.Count)
                {
                    // We already know that act -->+ other exists due to foreach
                    ApplyAndAdd(dcr, events, new RedundantRelationEvent(patternName, RelationType.Inclusion, act, other, round));
                    // Conditionally also remove the other way around (Avoids dual evaluation from the perspective of 'other' later)
                    if (other.Includes(dcr).Contains(act))
                        ApplyAndAdd(dcr, events, new RedundantRelationEvent(patternName, RelationType.Inclusion, other, act, round));
                }
            }

            return events;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// Searches for an N-tree of activities that per inclusion and immediate exclusion is executable at most once,
        /// and removes redundancies for activities in such a tree.
        /// </summary>
        public static List<RedundancyEvent> ApplySequentialSingularExecutionPattern(DcrGraphSimple dcr, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "SequentialSingularExecutionPattern";

            var singularlyExecutables = dcr.GetSequentialSingularExecutionActivityOrder();

            HashSet<Activity> previousLevelsActivities = new HashSet<Activity>();
            for (var i = 0; i < singularlyExecutables.Count; i++)
            {
                Activity act = singularlyExecutables[i];
                Activity prev = i > 0 ? singularlyExecutables[i - 1] : null;
                var futureActivities = new HashSet<Activity>(dcr.Activities.Except(previousLevelsActivities)
                    .Except(new List<Activity> {act}));

                // OUTGOING REDUNDANCIES:

                // Can remove any outgoing Exclusion that does not target oneself or the previous activity if that activity does not self-exclude
                ApplyAndAdd(dcr, events, act.Excludes(dcr)
                    .Where(e => !Equals(e, act) && (prev == null || prev.HasExcludeTo(prev, dcr) || !Equals(e, prev)))
                    .Select(e =>
                    new RedundantRelationEvent(patternName, RelationType.Exclusion, act, e, round)));

                // Can remove any forwards outgoing Response (leaving the level) which targets an initially Pending activity
                // + Can remove self-Response if self-excluding
                // + Can remove backwards Responses
                ApplyAndAdd(dcr, events, act.Responses(dcr)
                    .Where(r => (Equals(r, act) && act.HasExcludeTo(act, dcr)) || (!Equals(r, act) && r.Pending) || (previousLevelsActivities.Contains(r)))
                    .Select(other =>
                        new RedundantRelationEvent(patternName, RelationType.Response, act, other, round)));

                // Can remove all outgoing Conditions not targeting self nor targeting previous if it is not self-excluding
                ApplyAndAdd(dcr, events, act.Conditions(dcr)
                    .Where(e => !Equals(e, act) && (prev == null || prev.HasExcludeTo(prev, dcr) || !Equals(e, prev)))
                    .Select(e =>
                        new RedundantRelationEvent(patternName, RelationType.Condition, act, e, round)));

                // INCOMING REDUNDANCIES:

                // For Exclusions, we may not remove one such incoming Exclusion if that exclusion is the reason that this activity got to be part of a level.
                // ^--> This means that either we must self-exclude (in which case we can remove all future incoming excludes) or we mustn't be including that activity.
                // ... Naturally, never remove any self-exclusions
                ApplyAndAdd(dcr, events, act.ExcludesMe(dcr)
                    .Where(source => !source.Equals(act) && (act.HasExcludeTo(act, dcr) || !act.HasIncludeTo(source, dcr))).Select(other =>
                        new RedundantRelationEvent(patternName, RelationType.Exclusion, other, act, round)));
                // Other relation-types can be removed regardless of whether or not they were part of the last fringe (level-search-attempt)
                ApplyAndAdd(dcr, events, act.ResponsesMe(dcr).Where(x => futureActivities.Contains(x)).Select(
                    other =>
                        new RedundantRelationEvent(patternName, RelationType.Response, other, act, round)));
                ApplyAndAdd(dcr, events, act.ConditionsMe(dcr)
                    // Don't remove condition from future if it is an activity we include + we don't self-exclude
                    .Where(x => futureActivities.Contains(x) && (act.HasExcludeTo(act, dcr) || !act.HasIncludeTo(x, dcr))).Select(
                    other =>
                        new RedundantRelationEvent(patternName, RelationType.Condition, other, act, round)));


                previousLevelsActivities.Add(act);
            }

            return events;
        }

        private static bool TargetIsNotTargetingPreviousUnhealthily(DcrGraphSimple dcr, Activity x, Activity previous)
        {
            if (previous == null)
                return true;

            if (previous.HasExcludeTo(previous, dcr))
                return true;

            if (!Equals(x, previous))
                return true;

            return false;
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
                ApplyAndAdd(dcr, events, new RedundantRelationEvent(patternName, RelationType.Condition, act.IncludesMe(dcr).First(), act, round));
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
                if (!B.Included) continue; // Must be initially included

                if (B.Equals(C)) continue; // Don't treat self-conditions here

                // Nobody except C may have exclusion to B (We know C always happens after)
                if (B.ExcludesMe(dcr).Except(new List<Activity>{ C }).Any())
                    continue;

                foreach (var A in incomingConditionsC_Copy)
                {
                    if (A.Equals(C)) continue; // Don't treat self-conditions here
                    if (A.Equals(B)) continue; // A DIFFERENT incoming condition to C (therefore not B)

                    // A must not be both includable (incl. by B and C) and excludable from the "outside" of pattern
                    if (A.IncludesMe(dcr).Any()
                        && A.ExcludesMe(dcr).Except(new List<Activity> { B, C }).Any())
                        continue;

                    if (// [A] -->* [B]
                        A.Included
                        && A.Conditions(dcr).Contains(B)
                        && !C.IncludesMe(dcr).Any(x => x.Equals(A))
                        )
                    {
                        // The condition is redundant since we can only be included by the conditioner
                        ApplyAndAdd(dcr, events, new RedundantRelationEvent(patternName, RelationType.Condition, A, C, round));
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
                                      Equals(inclSourceB, C) ||
                                      false); //Chase(dcr, inclSourceB, C.IncludesMe(dcr), 4));
                }
                if (canDo)
                {
                    ApplyAndAdd(dcr, events, new RedundantRelationEvent(patternName, RelationType.Inclusion, B, C, round));
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

            
            foreach (var A in new HashSet<Activity>(C.ResponsesMe(dcr)))
            {
                // A should not exclude C:
                if (A.HasExcludeTo(C, dcr))
                    continue;

                if (ResponseChase(dcr, null, A, C, 4))
                {
                    ApplyAndAdd(dcr, events, new RedundantRelationEvent(pattern, RelationType.Response, A, C, round));
                }
            }

            return events;
        }

        public static Boolean ResponseChase(DcrGraphSimple dcr, Activity previous, Activity current, Activity target, int countdown)
        {
            if (countdown == 0)
                return false;

            // For non-initial activity; check for all necessary conditions for this step of the chain to be a valid chain-member:
            if (previous != null)
            {
                // Nobody should exclude current:
                if (current.ExcludesMe(dcr).Count > 0)
                    return false;

                // Need to be Included or have Include-relation from previous
                if (!current.Included && !previous.HasIncludeTo(current, dcr))
                    return false;

                // May not have exclude to target
                if (current.HasExcludeTo(target, dcr))
                    return false;

                // Now know that "current" is a valid chain-member, see if it has a response to target AND doesn't exclude target:
                if (current.HasResponseTo(target, dcr)) 
                {
                    return true;
                }
            }

            // Otherwise, keep chasing:
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
                ApplyAndAdd(dcr, events, act.Includes(dcr)
                    .Select(x => new RedundantRelationEvent(patternName, RelationType.Inclusion, act, x, round)));
                ApplyAndAdd(dcr, events, act.ExcludesMe(dcr).Select(x =>
                    new RedundantRelationEvent(patternName, RelationType.Exclusion, x, act, round)));
                ApplyAndAdd(dcr, events, act.Responses(dcr).Select(x =>
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
                    foreach (var intersectedActivity in new HashSet<Activity>(A.Conditions(dcr).Intersect(B.Conditions(dcr))))
                    {
                        ApplyAndAdd(dcr, events, new RedundantRelationEvent(patternName, RelationType.Condition, B, intersectedActivity, round));
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

            foreach (var B in new HashSet<Activity>(A.Includes(dcr)))
            {
                foreach (var C in B.IncludesMe(dcr))
                {
                    if (A.Equals(C))
                        continue;
                    if (C.HasConditionTo(B, dcr) && C.ExcludesMe(dcr).Count == 0)
                    {
                        ApplyAndAdd(dcr, events, new RedundantRelationEvent(patternName, RelationType.Inclusion, A, B, round));
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
                // Remove self-inclusions:
                if (act.Includes(dcr).Contains(act))
                {
                    ApplyAndAdd(dcr, events, new List<RedundancyEvent> { new RedundantRelationEvent(patternName, RelationType.Inclusion, act, act, round) });
                }

                // If excluded and never included
                if (!act.Included && act.IncludesMe(dcr).Count == 0)
                {
                    // Remove activity and all of its relations
                    ApplyAndAdd(dcr, events, act.Includes(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Inclusion, act, x, round)));
                    ApplyAndAdd(dcr, events, act.IncludesMe(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Inclusion, x, act, round)));
                    ApplyAndAdd(dcr, events, act.Excludes(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Exclusion, act, x, round)));
                    ApplyAndAdd(dcr, events, act.ExcludesMe(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Exclusion, x, act, round)));
                    ApplyAndAdd(dcr, events, act.Conditions(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Condition, act, x, round)));
                    ApplyAndAdd(dcr, events, act.ConditionsMe(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Condition, x, act, round)));
                    ApplyAndAdd(dcr, events, act.Responses(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Response, act, x, round)));
                    ApplyAndAdd(dcr, events, act.ResponsesMe(dcr).Select(x => new RedundantRelationEvent(patternName, RelationType.Response, x, act, round)));
                    ApplyAndAdd(dcr, events, new RedundantActivityEvent(patternName, act));
                    //Console.WriteLine($"Excluded activity rule: Removed {res.Removed.Count - before} relations in total");
                }
                // If included and never excluded --> remove all incoming includes
                else if (act.Included && act.ExcludesMe(dcr).Count == 0)
                {
                    // Remove all incoming includes
                    ApplyAndAdd(dcr, events, new HashSet<Activity>(act.IncludesMe(dcr)).Select(x =>
                        new RedundantRelationEvent(patternName, RelationType.Inclusion, x, act, round)));
                    //Console.WriteLine($"Always Included activity rule: Removed {res.Removed.Count - before} relations in total");
                }
            }

            return events;
        }

        #endregion
    }
}
