﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    public class RedundancyRemoverComparer
    {


        public void PerformComparison(DcrGraph dcr, BackgroundWorker bgWorker = null)
        {
            // Reset running-time measurements
            _methodRunningTimes = new Dictionary<string, TimeSpan>();

            // Convert to pattern-application-friendly type (exploiting efficiency of dual-dictionary structure)
            var dcrSimple = DcrGraphExporter.ExportToSimpleDcrGraph(dcr);
            
            // Pattern-application:
            var basicRelationsRemovedCount = 0;
            var patternRelationsRemoved = 0;
            var iterations = 0;
            DcrGraphSimple before;
            do
            {
                before = dcrSimple.Copy();
                // Basic-optimize simple-graph:
                var (basicRelationsRemovedCountThisRound, basicActivitiesRemovedCountThisRound)
                    = ApplyBasicRedundancyRemovalLogic(dcrSimple);
                basicRelationsRemovedCount += basicRelationsRemovedCountThisRound;
                patternRelationsRemoved += ApplyPatterns(dcrSimple);
                iterations++;
            }
            while (!before.Equals(dcrSimple));

            var totalPatternApproachRelationsRemoved = basicRelationsRemovedCount + patternRelationsRemoved;

            // Apply complete redundancy-remover and print when relations are redundant, that were not also removed in the Simple result.:
            var completeRemover = new RedundancyRemover();
            var rrGraph = completeRemover.RemoveRedundancy(dcr, bgWorker, dcrSimple);
            var redundantRelationsCount = completeRemover.RedundantRelationsFound;
            var redundantActivitiesCount = completeRemover.RedundantActivitiesFound;

            // Time-measurement results
            foreach (var kvPair in _methodRunningTimes)
            {
                Console.WriteLine($"{kvPair.Key}: {kvPair.Value:g}");
            }

            // Comparison
            Console.WriteLine(
                $"Pattern approach detected {(totalPatternApproachRelationsRemoved / (double)redundantRelationsCount):P2} " +
                $"({totalPatternApproachRelationsRemoved} / {redundantRelationsCount})");
            Console.WriteLine($"Patterns applied over {iterations} rounds.");

            Console.WriteLine($"Relations left using pattern-searcher: {dcrSimple.RelationsCount}");

            //Console.WriteLine("RELATIONS IN SIMPLE RESULT, NOT IN COMPLETE: (Un-caught redundancy)");

            // Inform about specific relations "missed" by pattern-approach
            //foreach (var act in dcrSimple.Activities)
            //{
            //    if (!dcrSimple.Includes.TryGetValue(act, out var inclTargets))
            //        continue;

            //    foreach (var include in inclTargets)
            //    {
            //        if (rrGraph.IncludeExcludes.TryGetValue(act, out Dictionary<Activity, Confidence> otherInclTargets))
            //        {
            //            if (otherInclTargets.TryGetValue(include, out var confidence))
            //            {
            //                if (!confidence.IsAboveThreshold())
            //                {
            //                    Console.WriteLine($"{act.Id} -->+ {include.Id} (1)");
            //                }
            //            }
            //            else // Neither include nor exclude
            //            {
            //                Console.WriteLine($"{act.Id} -->+ {include.Id} (2)");
            //            }
            //        }
            //    }
            //}

            // Export to XML
            Console.WriteLine("RESULT-DCR GRAPH:");
            Console.WriteLine(DcrGraphExporter.ExportToXml(dcrSimple));
        }

        /// <summary>
        /// Applies all our great patterns.
        /// </summary>
        /// <returns>Amount of relations removed</returns>
        private int ApplyPatterns(DcrGraphSimple dcr)
        {
            var relationsRemoved = 0;

            foreach (var act in dcr.Activities)
            {
                // "Outgoing relations of un-executable activities"
                relationsRemoved += ExecuteWithStatistics(ApplyRedundantRelationsFromUnExecutableActivityPattern, dcr, act);

                // "Always Included"


                // "Redundant Response"
                relationsRemoved += ExecuteWithStatistics(ApplyRedundantResponsePattern, dcr, act);

                // "Redundant Chain-Inclusion"
                relationsRemoved += ExecuteWithStatistics(ApplyRedundantChainInclusionPattern, dcr, act);

                // "Redundant Precedence"
                relationsRemoved += ExecuteWithStatistics(ApplyRedundantPrecedencePattern, dcr, act);

                // "Redundant 3-activity precedence"
                relationsRemoved += ExecuteWithStatistics(ApplyRedundantPrecedence3ActivitesPattern, dcr, act);

                // "Runtime excluded condition"
                relationsRemoved += ExecuteWithStatistics(ApplyLastConditionHoldsPattern, dcr, act);
            }

            return relationsRemoved;
        }

        private readonly bool _measureRunningTimes = true;
        private Dictionary<string, TimeSpan> _methodRunningTimes = new Dictionary<string, TimeSpan>();
        private T ExecuteWithStatistics<T>(Func<DcrGraphSimple, Activity, T> func, DcrGraphSimple dcr, Activity act)
        {
            if (!_measureRunningTimes)
                return func.Invoke(dcr, act);

            // Else: Perform running-time measurements and store them with the invoked method's name
            var start = DateTime.Now;
            var tResult = func.Invoke(dcr, act);
            var end = DateTime.Now;

            // Add the running time to the combined running time for this pattern-search method
            if (_methodRunningTimes.TryGetValue(func.Method.Name, out var runningTime))
                _methodRunningTimes[func.Method.Name] = runningTime.Add(end - start);
            else
                _methodRunningTimes.Add(func.Method.Name, end - start);
            //Console.WriteLine($"{func.Method.Name} took {end - start:g}");

            return tResult;
        }

        #region Pattern implementations

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// For graph G:
        /// A*--> [B!]
        /// , if B is never executable(meaning the initial Pending state is never removed).
        /// </summary>
        private int ApplyRedundantResponsePattern(DcrGraphSimple dcr, Activity act)
        {
            var relationsRemoved = 0;

            // If pending and never executable --> Remove all incoming Responses
            if (act.Pending && !dcr.IsEverExecutable(act))
            {
                // Remove all incoming Responses
                relationsRemoved += dcr.RemoveAllIncomingResponses(act);
                Console.WriteLine($"SHOULDNT HAPPEN UNTIL IsEverExecutable IS IMPLEMENTED");
            }

            return relationsRemoved;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// For graph G:
        /// [A] -->+ B
        /// [A] -->* B
        /// , if [A] -->+ B is the only inclusion to B, then the condition is redundant.
        /// </summary>
        private int ApplyRedundantPrecedencePattern(DcrGraphSimple dcr, Activity act)
        {
            var relationsRemoved = 0;

            if (!act.Included // This act is excluded
                // A single ingoing Inclusion
                && dcr.IncludesInverted.TryGetValue(act, out var incomingIncludes)
                && incomingIncludes.Count == 1
                // That same activity also has condition to us
                && dcr.ConditionsInverted.TryGetValue(act, out var incomingConditions)
                && incomingConditions.Contains(incomingIncludes.First()))
            {
                // The condition is redundant since we can only be included by the conditioner
                dcr.RemoveCondition(incomingIncludes.First(), act);
                relationsRemoved++;
            }

            return relationsRemoved;
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
        private int ApplyRedundantPrecedence3ActivitesPattern(DcrGraphSimple dcr, Activity C)
        {
            var relationsRemoved = 0;

            // At least two incoming conditions to C (A and B)
            if (!dcr.ConditionsInverted.TryGetValue(C, out var incomingConditionsC) || incomingConditionsC.Count < 2)
                return 0;

            var incomingConditionsC_Copy = new HashSet<Activity>(incomingConditionsC);

            foreach (var B in incomingConditionsC_Copy)
            {
                // No incoming exclusions to B
                if (dcr.ExcludesInverted.TryGetValue(B, out var incomingExclusionsB) &&
                    incomingExclusionsB.Count > 0)
                    continue;

                foreach (var A in incomingConditionsC_Copy)
                {
                    if (A.Equals(B)) continue; // Another incoming condition to C (therefore not B)

                    // A must not be both excludable and includable from activities other than B and C
                    if (dcr.IncludesInverted.TryGetValue(A, out var incomingIncludesA)
                        && incomingIncludesA.Except(new List<Activity> {B, C}).Any()
                        && dcr.ExcludesInverted.TryGetValue(A, out var incomingExcludesA)
                        && incomingExcludesA.Except(new List<Activity> {B, C}).Any())
                        continue;

                    if (// [A] -->* [B]
                        A.Included
                        && (dcr.Conditions.TryGetValue(A, out var outgoingConditionsA)
                            && outgoingConditionsA.Contains(B))
                        &&
                            (B.Included
                            ||
                            // If not included, A must include B
                            (dcr.Includes.TryGetValue(A, out var outgoingInclusionsA)
                            && outgoingInclusionsA.Contains(B)))
                        )
                    {
                        // The condition is redundant since we can only be included by the conditioner
                        dcr.RemoveCondition(A, C);
                        relationsRemoved++;
                    }
                }
            }

            return relationsRemoved;
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
        private int ApplyRedundantChainInclusionPattern(DcrGraphSimple dcr, Activity B)
        {
            if (B.Included) return 0; // B must be excluded

            var relationsRemoved = 0;

            // Iterate all ingoing inclusions to B - they must all also include C, in order for B -->+ C to be redundant
            if (!dcr.Includes.TryGetValue(B, out var inclTargetsB) || inclTargetsB.Count == 0
                || !dcr.IncludesInverted.TryGetValue(B, out var inclSourcesB) || inclSourcesB.Count == 0)
                return 0;

            // TODO: May avoid considering ingoing Inclusions to B from activities that are not executable at start-time.
            // TODO: - This would capture >=1 more case in mortgage graph, since such an includer shouldn't be required to
            // TODO:   also include C.

            foreach (var C in new HashSet<Activity>(inclTargetsB)) // Since we might remove inclusions, we cannot foreach the collection directly
            {
                // Skip if C is excludable
                if (dcr.ExcludesInverted.TryGetValue(C, out var exclSourcesC)
                    && exclSourcesC.Count > 0)
                    continue;

                if (dcr.IncludesInverted.TryGetValue(C, out var inclSourcesC))
                {
                    var intersection = inclSourcesB.Intersect(inclSourcesC);
                    // Anyone who includes B, should also Include C - thus the amount including B and C should be
                    // the same as the amount including B.
                    var intersectSize = intersection.Count();
                    if (intersectSize == inclSourcesB.Count
                        || (intersectSize == inclSourcesB.Count - 1 && inclSourcesB.Contains(C)))
                    {
                        dcr.RemoveInclude(B, C);
                        relationsRemoved++;
                    }
                }
            }

            return relationsRemoved;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// If an activity can never be executed, all of its after-execution relations have no effect,
        /// and can thus be removed.
        /// </summary>
        private int ApplyRedundantRelationsFromUnExecutableActivityPattern(DcrGraphSimple dcr, Activity act)
        {
            var relationsRemoved = 0;

            if (!dcr.IsEverExecutable(act)) // TODO: It is a task in itself to detect executability
            {
                relationsRemoved += dcr.RemoveAllOutgoingIncludes(act);
                relationsRemoved += dcr.RemoveAllIncomingExcludes(act);
                relationsRemoved += dcr.RemoveAllOutgoingResponses(act);

                // Note: Conditions still have effect
            }

            return relationsRemoved;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// 
        /// </summary>
        private int ApplyLastConditionHoldsPattern(DcrGraphSimple dcr, Activity A)
        {
            var relationsRemoved = 0;

            foreach (var B in dcr.Activities)
            {
                if (A.Included && B.Included
                    // A excludes B
                    && dcr.Excludes.TryGetValue(A, out var exclTargetsA) && exclTargetsA.Contains(B)
                    // Nobody excludes A
                    && dcr.NobodyExcludes(A)
                    // Nobody includes B (meaning excluded forever after A executes)
                    && dcr.NobodyIncludes(B)
                    // ... and they share an outgoing Condition target
                    && dcr.Conditions.TryGetValue(A, out var condTargetsA)
                    && dcr.Conditions.TryGetValue(B, out var condTargetsB))
                {
                    foreach (var intersectedActivity in condTargetsA.Intersect(condTargetsB))
                    {
                        dcr.RemoveCondition(B, intersectedActivity);
                        relationsRemoved++;
                    }
                }
            }

            return relationsRemoved;
        }

        #endregion

        private (int, int) ApplyBasicRedundancyRemovalLogic(DcrGraphSimple dcr)
        {
            var relationsRemoved = 0;
            var activitiesRemoved = 0;

            // Remove everything that is excluded and never included
            foreach (var act in dcr.Activities.ToArray())
            {
                // If excluded and never included
                if (!act.Included && !dcr.IncludesInverted.ContainsKey(act))
                {
                    // Remove activity and all of its relations
                    relationsRemoved += dcr.MakeActivityDisappear(act);
                    Console.WriteLine($"Excluded activity rule: Removed {relationsRemoved} relations in total");
                    activitiesRemoved++;
                }
                // If included and never excluded --> remove all incoming includes
                else if (act.Included && !dcr.ExcludesInverted.ContainsKey(act))
                {
                    // Remove all incoming includes
                    relationsRemoved += dcr.RemoveAllIncomingIncludes(act);
                    Console.WriteLine($"Always Included activity rule: Removed {relationsRemoved} relations in total");
                }
            }

            return (relationsRemoved, activitiesRemoved);
        }
    }
}
