using System;
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
            var dcrSimple = DcrGraphExporter.ExportToSimpleDcrGraph(dcr);

            // Basic-optimize simple-graph:
            var (basicRelationsRemovedCount, basicActivitiesRemovedCount)
                = ApplyBasicRedundancyRemovalLogic(dcrSimple); // Modifies simple
            //var basicRelationsRemovedCount = tuple.Item1;
            //var basicActivitiesRemovedCount = tuple.Item2;

            // Pattern-application:
            var patternRelationsRemoved = ApplyPatterns(dcrSimple);

            var totalPatternApproachRelationsRemoved = basicRelationsRemovedCount + patternRelationsRemoved;

            // Apply complete redundancy-remover and print when relations are redundant, that were not also removed in the Simple result.:
            var completeRemover = new RedundancyRemover();
            var rrGraph = completeRemover.RemoveRedundancy(dcr, bgWorker, dcrSimple);
            var redundantRelationsCount = completeRemover.RedundantRelationsFound;
            var redundantActivitiesCount = completeRemover.RedundantActivitiesFound;

            // Comparison
            Console.WriteLine(
                $"Pattern approach detected {(totalPatternApproachRelationsRemoved / (double)redundantRelationsCount):P2} " +
                $"({totalPatternApproachRelationsRemoved} / {redundantRelationsCount})");

            Console.WriteLine($"Relations left using pattern-searcher: {dcrSimple.RelationsCount}");

            Console.WriteLine("RELATIONS IN SIMPLE RESULT, NOT IN COMPLETE: (Un-caught redundancy)");

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
                // "Always Included"


                // "Redundant Response"
                relationsRemoved += ApplyRedundantResponsePattern(dcr, act);

                // "Redundant Chain-Inclusion"
                relationsRemoved += ApplyRedundantChainInclusionPattern(dcr, act);

                // "Redundant Precedence"
                relationsRemoved += ApplyRedundantPrecedencePattern(dcr, act);

                // "Outgoing relations of un-executable activities"
                relationsRemoved += ApplyRedundantRelationsFromUnExecutableActivityPattern(dcr, act);

                // "Runtime excluded condition"
                relationsRemoved += ApplyLastConditionHoldsPattern(dcr, act);
            }

            return relationsRemoved;
        }

        #region Pattern implementations

        /// <summary>
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
        /// For graph G:
        /// [A] -->+ B
        /// [A] -->* B
        /// , if [A] -->+ B is the only inclusion to B, then the condition is redundant.
        /// </summary>
        private int ApplyRedundantPrecedencePattern(DcrGraphSimple dcr, Activity act)
        {
            var relationsRemoved = 0;

            if (dcr.IncludesInverted.TryGetValue(act, out var incomingIncludes)
                && incomingIncludes.Count == 1
                && dcr.ConditionsInverted.TryGetValue(act, out var incomingConditions)
                && incomingConditions.Contains(incomingIncludes.First()))
            {
                dcr.RemoveCondition(incomingIncludes.First(), act);
                relationsRemoved++;
            }

            return relationsRemoved;
        }

        /// <summary>
        /// For graph G: [Example with inclusion to Excluded activity dependency]
        /// [A] -->+ B -->+ C
        /// [A] -->+ C
        /// (Only A is included at first)
        /// , if [A] -->+ B is the only inclusion to B, then B -->+ C is redundant.
        /// </summary>
        private int ApplyRedundantChainInclusionPattern(DcrGraphSimple dcr, Activity A)
        {
            // TODO: Move DependsOn to Activity? e.g. if (activityVariableA.DependsOn(activityVariableB)) { /* something */ }
            var relationsRemoved = 0;

            foreach (var B in dcr.Activities)
            {
                if (dcr.DependsOn(B, dependsOnAct: A)
                    // ... and they share an outgoing Inclusion target
                    && dcr.Includes.TryGetValue(A, out var inclTargetsA)
                    && dcr.Includes.TryGetValue(B, out var inclTargetsB))
                {
                    foreach (var intersectedActivity in inclTargetsA.Intersect(inclTargetsB))
                    {
                        dcr.RemoveInclude(B, intersectedActivity);
                        relationsRemoved++;
                    }
                }
            }

            return relationsRemoved;
        }

        private int ApplyRedundantRelationsFromUnExecutableActivityPattern(DcrGraphSimple dcr, Activity act)
        {
            var relationsRemoved = 0;

            if (!dcr.IsEverExecutable(act))
            {
                relationsRemoved += dcr.RemoveAllOutgoingIncludes(act);
                relationsRemoved += dcr.RemoveAllIncomingExcludes(act);
                relationsRemoved += dcr.RemoveAllOutgoingResponses(act);

                // Note: Conditions still have effect
            }

            return relationsRemoved;
        }

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
