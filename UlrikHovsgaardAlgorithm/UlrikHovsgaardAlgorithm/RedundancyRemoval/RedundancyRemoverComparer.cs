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
        public string Pattern;
        public Relation(string t, Activity s, Activity tar, string pattern = null)
        {
            Type = t;
            Source = s;
            Target = tar;
            Pattern = pattern;
        }

        public override string ToString()
        {
            return $"{Type} from {Source.Id} to {Target.Id} {(Pattern == null ? "" : $"by {Pattern}")}";
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
        public HashSet<Relation> MissingRedundantRelations { get; private set; } = new HashSet<Relation>();
        public HashSet<Relation> ErroneouslyRemovedRelations { get; private set; } = new HashSet<Relation>();
        public Dictionary<string, HashSet<Result>> AllResults { get; private set; } = new Dictionary<string, HashSet<Result>>();
        public int RoundsSpent { get; private set; }

        public DcrGraphSimple InitialGraph { get; private set; }
        public DcrGraphSimple FinalPatternGraph { get; private set; }
        public DcrGraph FinalCompleteGraph { get; private set; }


        #region statistics

        public int RedundantRelationsCountPatternApproach => AllResults.Values.Sum(x => x.Sum(y => y.Removed.Count));

        public int RedundantRelationsCountActual { get; private set; }

        public IEnumerable<string> Patterns => AllResults.Keys;

        public HashSet<Result> RemovedByPattern(string pattern)
        {
            return AllResults[pattern];
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

            RoundsSpent = iterations;

            foreach (var result in results)
            {
                if (AllResults.TryGetValue(result.PatternName, out var alreadyAdded))
                {
                    alreadyAdded.Add(result);
                }
                else
                {
                    var set = new HashSet<Result>();
                    set.Add(result);
                    AllResults.Add(result.PatternName, set);
                }
            }

            FinalPatternGraph = dcrSimple;

            var totalPatternApproachRelationsRemoved = results.Sum(x => x.Removed.Count);

            // Apply complete redundancy-remover and print when relations are redundant, that were not also removed in the Simple result.:
            var completeRemover = new RedundancyRemover();
            var (rrGraph, redundantRelations) = completeRemover.RemoveRedundancyInner(dcr, bgWorker, dcrSimple);
            FinalCompleteGraph = rrGraph;
            MissingRedundantRelations = redundantRelations;
            RedundantRelationsCountActual = completeRemover.RedundantRelationsFound;
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
                $"Pattern approach detected {(totalPatternApproachRelationsRemoved / (double)RedundantRelationsCountActual):P2} " +
                $"({totalPatternApproachRelationsRemoved} / {RedundantRelationsCountActual})");
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
                //set.Add(ExecuteWithStatistics(ApplyRedundantIncludeWhenIncludeConditionExistsPattern, dcr, act,
                  //  iterations));
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
                && act.IncludesMe(dcr).Count == 1
                // That same activity also has condition to us
                && act.ConditionsMe(dcr).Contains(act.IncludesMe(dcr).First()))
            {
                // The condition is redundant since we can only be included by the conditioner
                dcr.RemoveCondition(act.IncludesMe(dcr).First(), act);
                res.Removed.Add(new Relation("Condition", act.IncludesMe(dcr).First(), act, res.PatternName));
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
            if (C.ConditionsMe(dcr).Count < 2)
                return res;

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
                    if (A.IncludesMe(dcr).Except(new List<Activity> {B, C}).Any()
                        && A.ExcludesMe(dcr).Except(new List<Activity> {B, C}).Any())
                        continue;

                    if (// [A] -->* [B]
                        A.Included
                        && A.Conditions(dcr).Contains(B) &&
                            (B.Included ||
                            // If not included, A must include B
                            A.Includes(dcr).Contains(B))
                        )
                    {
                        // The condition is redundant since we can only be included by the conditioner
                        dcr.RemoveCondition(A, C);
                        res.Removed.Add(new Relation("Condition", A, C, res.PatternName));
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
            if (B.Includes(dcr).Count == 0 || B.IncludesMe(dcr).Count == 0)
                return res;

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
                        Equals(inclSourceB, C) || chase(dcr, inclSourceB, C.IncludesMe(dcr), 4));
                }
                if (canDo)
                {
                    dcr.RemoveInclude(B, C);
                    res.Removed.Add(new Relation("Include", B, C, res.PatternName));
                }
                
            }

            return res;
        }

        private Boolean chase(DcrGraphSimple dcr, Activity A, HashSet<Activity> mustInclude, int countdown)
        {
            if (countdown == 0)
                return false;
            var isFine = true;
            foreach (var incoming in A.IncludesMe(dcr))
            {
                isFine = isFine && (mustInclude.Contains(incoming) ||
                                    chase(dcr, incoming, mustInclude, countdown - 1));
            }
            return isFine;
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
                    && A.Excludes(dcr).Contains((B))
                    // Nobody excludes A
                    && dcr.NobodyExcludes(A)
                    // Nobody includes B (meaning excluded forever after A executes)
                    && dcr.NobodyIncludes(B))
                    // ... and they share an outgoing Condition target
                {
                    foreach (var intersectedActivity in A.Conditions(dcr).Intersect(B.Conditions(dcr)))
                    {
                        dcr.RemoveCondition(B, intersectedActivity);
                        res.Removed.Add(new Relation("Condition", B, intersectedActivity, res.PatternName));
                    }
                }
            }

            return res;
        }

        public Result ApplyRedundantIncludeWhenIncludeConditionExistsPattern(DcrGraphSimple dcr, Activity A, int round)
        {
            var res = new Result();
            res.PatternName = "RedundantIncludeWhenIncludeConditionExists";
            res.Removed = new HashSet<Relation>();
            res.Round = round;

            foreach (var B in A.Includes(dcr))
            {
                foreach (var C in B.IncludesMe(dcr))
                {
                    if (A.Equals(C))
                        continue;
                    if (C.HasConditionTo(B, dcr) && C.ExcludesMe(dcr).Count == 0)
                    {
                        res.Removed.Add(new Relation("include", A, B, res.PatternName));
                        dcr.RemoveInclude(A, B);
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
                if (!act.Included && act.IncludesMe(dcr).Count == 0)
                {
                    // Remove activity and all of its relations
                    var before = res.Removed.Count;
                    dcr.MakeActivityDisappear(act, res);
                    Console.WriteLine($"Excluded activity rule: Removed {res.Removed.Count - before} relations in total");
                }
                // If included and never excluded --> remove all incoming includes
                else if (act.Included && act.ExcludesMe(dcr).Count == 0)
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
            ErroneouslyRemovedRelations = new HashSet<Relation>();

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
                            ErroneouslyRemovedRelations.Add(GetRelationInAllResults(source, inclExclTarget, new List<string>{ "Include", "Exclude" }));
                            Console.WriteLine($"ERROR --> Include/Exclude from {source.Id} to {inclExclTarget.Id} removed faultily.");
                        }
                    }
                }

                if (graph.Responses.TryGetValue(source, out var responseTargets))
                {
                    foreach (var responseTarget in DcrGraph.FilterDictionaryByThreshold(responseTargets))
                    {
                        if (!dcrSimple.Responses.TryGetValue(source, out HashSet<Activity> otherResponseTargets)
                             || !otherResponseTargets.Contains(responseTarget))
                        {
                            ErroneouslyRemovedRelations.Add(GetRelationInAllResults(source, responseTarget, new List<string> { "Response" }));
                            Console.WriteLine($"ERROR --> Response from {source.Id} to {responseTarget.Id} removed faultily.");
                        }
                    }
                }

                if (graph.Conditions.TryGetValue(source, out var conditionTargets))
                {
                    foreach (var conditionTarget in DcrGraph.FilterDictionaryByThreshold(conditionTargets))
                    {
                        if (!dcrSimple.Conditions.TryGetValue(source, out HashSet<Activity> otherConditionTargets)
                            || !otherConditionTargets.Contains(conditionTarget))
                        {
                            ErroneouslyRemovedRelations.Add(GetRelationInAllResults(source, conditionTarget, new List<string> { "Condition" }));
                            Console.WriteLine($"ERROR --> Response from {source.Id} to {conditionTarget.Id} removed faultily.");
                        }
                    }
                }
            }
        }

        private Relation GetRelationInAllResults(Activity source, Activity target, List<string> relationsWanted)
        {
            return AllResults.Values.SelectMany(x => x)
                .SelectMany(x => x.Removed).First(relation =>
                    relation.Source.Id == source.Id && relation.Target.Id == target.Id &&
                    relationsWanted.Contains(relation.Type));
        }

        #endregion
    }
}
