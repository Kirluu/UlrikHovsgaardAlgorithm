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
    public struct Relation
    {
        public string Type;
        public Activity Source, Target;
        public Relation(string t, Activity s, Activity tar)
        {
            Type = t;
            Source = s;
            Target = tar;
        }
    }
    public struct Result
    {
        public string PatternName;
        public int Round { get; set; }
        public HashSet<Relation> Removed { get; set; }
    }
    public class RedundancyRemoverComparer
    {
        public HashSet<Relation> MissingRedundantRelations { get; private set; }
        private readonly Dictionary<string, HashSet<Result>> allResults = new Dictionary<string, HashSet<Result>>();

        public DcrGraphSimple InitialGraph { get; private set; }
        public DcrGraphSimple FinalPatternGraph { get; private set; }
        public DcrGraph FinalCompleteGraph { get; private set; }

        #region statistics

        public int RemovedCount()
        {
            return allResults.Values.Sum(x => x.Count);
        }

        public IEnumerable<string> Patterns()
        {
            return allResults.Keys;
        }

        public HashSet<Result> RemovedByPattern(string pattern)
        {
            return allResults[pattern];
        }

        public Dictionary<string, HashSet<Result>> GetAllResults()
        {
            return allResults;
        }

        #endregion

        public void PerformComparison(DcrGraph dcr, BackgroundWorker bgWorker = null)
        {
            // Reset running-time measurements
            _methodRunningTimes = new Dictionary<string, TimeSpan>();

            // Convert to pattern-application-friendly type (exploiting efficiency of dual-dictionary structure)
            var dcrSimple = DcrGraphExporter.ExportToSimpleDcrGraph(dcr);
            
            // Pattern-application:
            InitialGraph = dcrSimple.Copy();
            var iterations = 0;
            DcrGraphSimple before;
            HashSet<Result> results = new HashSet<Result>();
            do
            {
                before = dcrSimple.Copy();
                // Basic-optimize simple-graph:
                results.Add(ApplyBasicRedundancyRemovalLogic(dcrSimple, iterations));
                foreach (var r in ApplyPatterns(dcrSimple, iterations))
                {
                    results.Add(r);
                }
                iterations++;
            }
            while (!before.Equals(dcrSimple));
            foreach (var result in results)
            {
                if (allResults.TryGetValue(result.PatternName, out var alreadyAdded))
                {
                    alreadyAdded.Add(result);
                }
                else
                {
                    var set = new HashSet<Result>();
                    set.Add(result);
                    allResults.Add(result.PatternName, set);
                }
            }

            FinalPatternGraph = dcrSimple;

            var totalPatternApproachRelationsRemoved = results.Sum(x => x.Removed.Count);

            // Apply complete redundancy-remover and print when relations are redundant, that were not also removed in the Simple result.:
            var completeRemover = new RedundancyRemover();
            var (rrGraph, redundantRelations) = completeRemover.RemoveRedundancyInner(dcr, bgWorker, dcrSimple);
            FinalCompleteGraph = rrGraph;
            MissingRedundantRelations = redundantRelations;
            var redundantRelationsCount = completeRemover.RedundantRelationsFound;
            var redundantActivitiesCount = completeRemover.RedundantActivitiesFound;

            // Time-measurement results
            Console.WriteLine("-------------------------------------------------------------");
            foreach (var kvPair in _methodRunningTimes)
            {
                Console.WriteLine($"{kvPair.Key}: {kvPair.Value:g}");
            }
            Console.WriteLine("-------------------------------------------------------------\n");

            // Comparison
            Console.WriteLine(
                $"Pattern approach detected {(totalPatternApproachRelationsRemoved / (double)redundantRelationsCount):P2} " +
                $"({totalPatternApproachRelationsRemoved} / {redundantRelationsCount})");
            Console.WriteLine($"Patterns applied over {iterations} rounds.");

            Console.WriteLine($"Relations left using pattern-searcher: {dcrSimple.RelationsCount}");
            
            // Check for, and inform about relations removed by pattern-approach, but not the complete redudancy-remover
            PrintRelationsInDcrGraphNotInDcrGraphSimple(rrGraph, dcrSimple);

            // Export to XML
            Console.WriteLine("RESULT-DCR GRAPH:");
            Console.WriteLine(DcrGraphExporter.ExportToXml(dcrSimple));
        }

        /// <summary>
        /// Applies all our great patterns.
        /// </summary>
        /// <returns>Amount of relations removed</returns>
        private HashSet<Result> ApplyPatterns(DcrGraphSimple dcr, int iterations)
        {
            var set = new HashSet<Result>();

            foreach (var act in dcr.Activities)
            {
                // "Outgoing relations of un-executable activities"
                set.Add(ExecuteWithStatistics(ApplyRedundantRelationsFromUnExecutableActivityPattern, dcr, act, iterations));

                // "Always Included"


                // "Redundant Response"
                set.Add(ExecuteWithStatistics(ApplyRedundantResponsePattern, dcr, act, iterations));

                // "Redundant Chain-Inclusion"
                set.Add(ExecuteWithStatistics(ApplyRedundantChainInclusionPattern, dcr, act, iterations));

                // "Redundant Precedence"
                set.Add(ExecuteWithStatistics(ApplyRedundantPrecedencePattern, dcr, act, iterations));

                // "Redundant 3-activity precedence"
                set.Add(ExecuteWithStatistics(ApplyRedundantPrecedence3ActivitesPattern, dcr, act, iterations));

                // "Runtime excluded condition"
                set.Add(ExecuteWithStatistics(ApplyLastConditionHoldsPattern, dcr, act, iterations));
            }

            return set;
        }

        private readonly bool _measureRunningTimes = true;
        private Dictionary<string, TimeSpan> _methodRunningTimes = new Dictionary<string, TimeSpan>();
        private T ExecuteWithStatistics<T>(Func<DcrGraphSimple, Activity, int, T> func, DcrGraphSimple dcr, Activity act, int round)
        {
            if (!_measureRunningTimes)
                return func.Invoke(dcr, act, round);

            // Else: Perform running-time measurements and store them with the invoked method's name
            var start = DateTime.Now;
            var tResult = func.Invoke(dcr, act, round);
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
        private Result ApplyRedundantResponsePattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var res = new Result();
            res.Round = round;
            res.PatternName = "RedundantResponsePattern";
            var set = new HashSet<Relation>();
            res.Removed = set;

            // If pending and never executable --> Remove all incoming Responses
            if (act.Pending && !dcr.IsEverExecutable(act))
            {
                // Remove all incoming Responses
                dcr.RemoveAllIncomingResponses(act, res);
                Console.WriteLine($"SHOULDNT HAPPEN UNTIL IsEverExecutable IS IMPLEMENTED");
            }

            return res;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// For graph G:
        /// [A] -->+ B
        /// [A] -->* B
        /// , if [A] -->+ B is the only inclusion to B, then the condition is redundant.
        /// </summary>
        private Result ApplyRedundantPrecedencePattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var res = new Result();
            res.PatternName = "RedundantPrecedencePattern";
            res.Round = round;
            res.Removed = new HashSet<Relation>();

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
                res.Removed.Add(new Relation("condition", incomingIncludes.First(), act));
            }

            return res;
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
        private Result ApplyRedundantPrecedence3ActivitesPattern(DcrGraphSimple dcr, Activity C, int round)
        {
            var res = new Result();
            res.PatternName = "RedundantPrecedence3ActivitiesPattern";
            res.Removed = new HashSet<Relation>();
            res.Round = round;

            // At least two incoming conditions to C (A and B)
            if (!dcr.ConditionsInverted.TryGetValue(C, out var incomingConditionsC) || incomingConditionsC.Count < 2)
                return res;

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
                        res.Removed.Add(new Relation("condition", A, C));
                    }
                }
            }

            return res;
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
        private Result ApplyRedundantChainInclusionPattern(DcrGraphSimple dcr, Activity B, int round)
        {
            var res = new Result();
            res.Round = round;
            res.PatternName = "RedundantChainInclusionPattern";
            res.Removed = new HashSet<Relation>();
            if (B.Included) return res; // B must be excluded

            // Iterate all ingoing inclusions to B - they must all also include C, in order for B -->+ C to be redundant
            if (!dcr.Includes.TryGetValue(B, out var inclTargetsB) || inclTargetsB.Count == 0
                || !dcr.IncludesInverted.TryGetValue(B, out var inclSourcesB) || inclSourcesB.Count == 0)
                return res;

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
                    var canDo = true;
                    foreach (var inclSourceB in inclSourcesB)
                    {
                        canDo = canDo && (inclSourcesC.Contains(inclSourceB) ||
                            Equals(inclSourceB, C) || chase(dcr, inclSourceB, inclSourcesC, 4));
                    }
                    if (canDo)
                    {
                        dcr.RemoveInclude(B, C);
                        res.Removed.Add(new Relation("include", B, C));
                    }
                }
            }

            return res;
        }

        private Boolean chase(DcrGraphSimple dcr, Activity A, HashSet<Activity> mustInclude, int countdown)
        {
            if (countdown == 0)
                return false;
            if (dcr.IncludesInverted.TryGetValue(A, out var incomings) && incomings.Count > 0)
            {
                var isFine = true;
                foreach (var incoming in incomings)
                {
                    isFine = isFine && (mustInclude.Contains(incoming) ||
                                        chase(dcr, incoming, mustInclude, countdown - 1));
                }
                return isFine;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// If an activity can never be executed, all of its after-execution relations have no effect,
        /// and can thus be removed.
        /// </summary>
        private Result ApplyRedundantRelationsFromUnExecutableActivityPattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var res = new Result();
            res.PatternName = "RedundantRelationsFromExecutableActivityPattern";
            res.Round = round;
            res.Removed = new HashSet<Relation>();

            if (!dcr.IsEverExecutable(act)) // TODO: It is a task in itself to detect executability
            {
                dcr.RemoveAllOutgoingIncludes(act, res);
                dcr.RemoveAllIncomingExcludes(act, res);
                dcr.RemoveAllOutgoingResponses(act, res);
                // Note: Conditions still have effect
            }

            return res;
        }

        /// <summary>
        /// ORIGIN: Thought.
        /// 
        /// 
        /// </summary>
        private Result ApplyLastConditionHoldsPattern(DcrGraphSimple dcr, Activity A, int round)
        {
            var res = new Result();
            res.PatternName = "LastConditionHoldsPattern";
            res.Round = round;
            res.Removed = new HashSet<Relation>();

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
                        res.Removed.Add(new Relation("condition", B, intersectedActivity));
                    }
                }
            }

            return res;
        }

        #endregion

            private Result ApplyBasicRedundancyRemovalLogic(DcrGraphSimple dcr, int round)
        {
            var res = new Result();
            res.PatternName = "BasicRedundancyRemovalLogic";
            res.Removed = new HashSet<Relation>();
            res.Round = round;

            // Remove everything that is excluded and never included
            foreach (var act in dcr.Activities.ToArray())
            {
                // If excluded and never included
                if (!act.Included && !dcr.IncludesInverted.ContainsKey(act))
                {
                    // Remove activity and all of its relations
                    var before = res.Removed.Count;
                    dcr.MakeActivityDisappear(act, res);
                    Console.WriteLine($"Excluded activity rule: Removed {res.Removed.Count - before} relations in total");
                }
                // If included and never excluded --> remove all incoming includes
                else if (act.Included && !dcr.ExcludesInverted.ContainsKey(act))
                {
                    // Remove all incoming includes
                    var before = res.Removed.Count;
                    dcr.RemoveAllIncomingIncludes(act, res);
                    Console.WriteLine($"Always Included activity rule: Removed {res.Removed.Count - before} relations in total");
                }
            }

            return res;
        }

        #region Utility methods

        private void PrintRelationsInDcrGraphNotInDcrGraphSimple(DcrGraph graph, DcrGraphSimple dcrSimple)
        {
            // Check for, and inform about relations removed by pattern-approach, but not the complete redudancy-remover
            foreach (var source in graph.Activities)
            {
                if (graph.IncludeExcludes.TryGetValue(source, out var inclExclTargets))
                {
                    foreach (var inclExclTarget in inclExclTargets.Keys)
                    {
                        if ((!dcrSimple.Includes.TryGetValue(source, out HashSet<Activity> otherInclTargets)
                             || !otherInclTargets.Contains(inclExclTarget))
                            && (!dcrSimple.Excludes.TryGetValue(source, out HashSet<Activity> otherExclTargets)
                                || !otherExclTargets.Contains(inclExclTarget)))
                        {
                            Console.WriteLine($"ERROR --> Include/Exclude from {source.Id} to {inclExclTarget.Id} removed faultily.");
                        }
                    }
                }

                if (graph.Responses.TryGetValue(source, out var responseTargets))
                {
                    foreach (var responseTarget in responseTargets.Keys)
                    {
                        if (!dcrSimple.Responses.TryGetValue(source, out HashSet<Activity> otherResponseTargets)
                             || !otherResponseTargets.Contains(responseTarget))
                        {
                            Console.WriteLine($"ERROR --> Response from {source.Id} to {responseTarget.Id} removed faultily.");
                        }
                    }
                }

                if (graph.Conditions.TryGetValue(source, out var conditionTargets))
                {
                    foreach (var conditionTarget in conditionTargets.Keys)
                    {
                        if (!dcrSimple.Conditions.TryGetValue(source, out HashSet<Activity> otherConditionTargets)
                            || !otherConditionTargets.Contains(conditionTarget))
                        {
                            Console.WriteLine($"ERROR --> Response from {source.Id} to {conditionTarget.Id} removed faultily.");
                        }
                    }
                }
            }
        }

        #endregion
    }
}
