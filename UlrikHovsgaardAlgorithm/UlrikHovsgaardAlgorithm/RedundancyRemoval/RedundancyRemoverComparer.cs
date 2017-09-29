using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.QualityMeasures;
using UlrikHovsgaardAlgorithm.Utils;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    public abstract class RedundancyEvent
    {
        public abstract string Pattern { get; }
    }
    
    public class RedundantRelationEvent : RedundancyEvent
    {
        public override string Pattern { get; }
        public RelationType Type { get; }
        public Activity From { get; }
        public Activity To { get; }
        public int Round { get; }

        public RedundantRelationEvent(string pattern, RelationType type, Activity from, Activity to, int round)
        {
            Pattern = pattern;
            Type = type;
            From = from;
            To = to;
            Round = round;
        }

        public override string ToString()
        {
            return $"{Type} from {From} to {To} by {Pattern}";
        }
    }

    public class RedundantActivityEvent : RedundancyEvent
    {
        public override string Pattern { get; }
        public Activity Activity { get; }

        public RedundantActivityEvent(string pattern, Activity activity)
        {
            Pattern = pattern;
            Activity = activity;
        }
    }
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
    public class RedundancyRemoverComparer
    {
        public HashSet<Relation> MissingRedundantRelations { get; private set; } = new HashSet<Relation>();
        public HashSet<RedundantRelationEvent> ErroneouslyRemovedRelations { get; private set; } = new HashSet<RedundantRelationEvent>();
        public List<RedundancyEvent> AllResults { get; private set; } = new List<RedundancyEvent>();
        public int RoundsSpent { get; private set; }

        public DcrGraphSimple InitialGraph { get; private set; }
        public DcrGraphSimple FinalPatternGraph { get; private set; }
        public DcrGraph FinalCompleteGraph { get; private set; }

        public DcrGraphSimple ApplyEvents(List<RedundancyEvent> events)
        {
            var graph = InitialGraph.Copy();
            foreach (var redundancyEvent in events)
            {
                switch (redundancyEvent) 
                {
                    case RedundantActivityEvent ract:
                        graph.MakeActivityDisappear(ract.Activity);
                        break;
                    case RedundantRelationEvent rr when rr.Type == RelationType.Condition:
                        graph.RemoveCondition(rr.From, rr.To);
                        break;
                    case RedundantRelationEvent rr when rr.Type == RelationType.Condition:
                        graph.RemoveInclude(rr.From, rr.To);
                        break;
                    case RedundantRelationEvent rr when rr.Type == RelationType.Condition:
                        graph.RemoveResponse(rr.From, rr.To);
                        break;
                    case RedundantRelationEvent rr when rr.Type == RelationType.Condition:
                        graph.RemoveExclude(rr.From, rr.To);
                        break;
                }
            }
            return graph;
        }

        public List<RedundancyEvent> GetEventsUpUntil(RedundancyEvent untilEvent)
        {
            var index = AllResults.FindIndex(x => x == untilEvent);
            return AllResults.GetRange(0, index); // All events excluding the given event
        }

        public DcrGraphSimple GetContextBeforeEvent(RedundancyEvent contextEvent)
        {
            return ApplyEvents(GetEventsUpUntil(contextEvent));
        }

        #region statistics

        public int RedundantRelationsCountPatternApproach => AllResults.Where(x => x is RedundantRelationEvent).Sum(x => 1);

        public int RedundantRelationsCountActual { get; private set; }

        public IEnumerable<string> Patterns => AllResults.Select(x => x.Pattern).Distinct();

        public IEnumerable<RedundancyEvent> RemovedByPattern(string pattern)
        {
            return AllResults.Where(x => pattern.Equals(x.Pattern));
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
            List<RedundancyEvent> results = new List<RedundancyEvent>();
            do
            {
                before = dcrSimple.Copy();
                // Basic-optimize simple-graph:
                results.AddRange(ApplyBasicRedundancyRemovalLogic(dcrSimple, iterations));
                results.AddRange(ApplyPatterns(dcrSimple, iterations));
                iterations++;
            }
            while (!before.Equals(dcrSimple));

            foreach (var res in results)
            {
                Console.WriteLine($"{res.Pattern}:");
                switch (res)
                {
                    case RedundantRelationEvent rre:
                        Console.WriteLine($"{rre.Type} from {rre.From} to {rre.To}");
                        break;
                    case RedundantActivityEvent rre:
                        break;
                }
                Console.WriteLine("--------------------");
            }

            RoundsSpent = iterations;

            AllResults = results;

            FinalPatternGraph = dcrSimple;

            var totalPatternApproachRelationsRemoved = results.Sum(x => 1);

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

            /* TODO: The final comparison should comprise of a trace-comparsion to see whether the "erroneous" relations removed
               are actually erroneous.*/

            var comparerTraceFinder = new UniqueTraceFinder(new ByteDcrGraph(FinalCompleteGraph));
            var sameLanguage = comparerTraceFinder.CompareTraces(new ByteDcrGraph(dcrSimple));

            Console.WriteLine($"Is the graph language the same?!: {sameLanguage}");

            // Export to XML
            Console.WriteLine("RESULT-DCR GRAPH:");
            //Console.WriteLine(DcrGraphExporter.ExportToXml(dcrSimple));
        }

        /// <summary>
        /// Applies all our great patterns.
        /// </summary>
        /// <returns>Amount of relations removed</returns>
        private List<RedundancyEvent> ApplyPatterns(DcrGraphSimple dcr, int iterations)
        {
            var events = new List<RedundancyEvent>();

            foreach (var act in dcr.Activities)
            {
                // "Outgoing relations of un-executable activities"
                events.AddRange(ExecuteWithStatistics(ApplyRedundantRelationsFromUnExecutableActivityPattern, dcr, act, iterations));

                // "Always Included"


                // "Redundant Response"
                events.AddRange(ExecuteWithStatistics(ApplyRedundantResponsePattern, dcr, act, iterations));

                // "Redundant Chain-Inclusion"
                events.AddRange(ExecuteWithStatistics(ApplyRedundantChainInclusionPattern, dcr, act, iterations));

                // "Redundant Precedence"
                events.AddRange(ExecuteWithStatistics(ApplyRedundantPrecedencePattern, dcr, act, iterations));

                // "Redundant 3-activity precedence"
                events.AddRange(ExecuteWithStatistics(ApplyRedundantPrecedence3ActivitesPattern, dcr, act, iterations));

                // "Runtime excluded condition"
                events.AddRange(ExecuteWithStatistics(ApplyLastConditionHoldsPattern, dcr, act, iterations));
                //set.Add(ExecuteWithStatistics(ApplyRedundantIncludeWhenIncludeConditionExistsPattern, dcr, act,
                  //  iterations));
            }

            return events;
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
        private List<RedundancyEvent> ApplyRedundantResponsePattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "RedundantResponsePattern";

            // If pending and never executable --> Remove all incoming Responses
            if (act.Pending && !dcr.IsEverExecutable(act))
            {
                // Remove all incoming Responses
                dcr.RemoveAllIncomingResponses(act);
                events.AddRange(act.ResponsesMe(dcr).Select(x =>
                    new RedundantRelationEvent(patternName, RelationType.Response, x, act, round)));
                Console.WriteLine($"SHOULDNT HAPPEN UNTIL IsEverExecutable IS IMPLEMENTED");
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
        private List<RedundancyEvent> ApplyRedundantPrecedencePattern(DcrGraphSimple dcr, Activity act, int round)
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
                dcr.RemoveCondition(act.IncludesMe(dcr).First(), act);
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
        private List<RedundancyEvent> ApplyRedundantPrecedence3ActivitesPattern(DcrGraphSimple dcr, Activity C, int round)
        {
            var events = new List<RedundancyEvent>();
            var patternName = "RedundantPrecedence3ActivitiesPattern";

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
        private List<RedundancyEvent> ApplyRedundantChainInclusionPattern(DcrGraphSimple dcr, Activity B, int round)
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
                        Equals(inclSourceB, C) || chase(dcr, inclSourceB, C.IncludesMe(dcr), 4));
                }
                if (canDo)
                {
                    dcr.RemoveInclude(B, C);
                    events.Add(new RedundantRelationEvent(patternName, RelationType.Inclusion, B, C, round));
                }
                
            }

            return events;
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
        private List<RedundancyEvent> ApplyRedundantRelationsFromUnExecutableActivityPattern(DcrGraphSimple dcr, Activity act, int round)
        {
            var patternName = "RedundantRelationsFromExecutableActivityPattern";
            var events = new List<RedundancyEvent>();

            if (!dcr.IsEverExecutable(act)) // TODO: It is a task in itself to detect executability
            {
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
        /// 
        /// </summary>
        private List<RedundancyEvent> ApplyLastConditionHoldsPattern(DcrGraphSimple dcr, Activity A, int round)
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
                    // ... and they share an outgoing Condition target
                {
                    foreach (var intersectedActivity in A.Conditions(dcr).Intersect(B.Conditions(dcr)))
                    {
                        dcr.RemoveCondition(B, intersectedActivity);
                        events.Add(new RedundantRelationEvent(patternName, RelationType.Condition, B, intersectedActivity, round));
                    }
                }
            }

            return events;
        }

        public List<RedundancyEvent> ApplyRedundantIncludeWhenIncludeConditionExistsPattern(DcrGraphSimple dcr, Activity A, int round)
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

        private List<RedundancyEvent> ApplyBasicRedundancyRemovalLogic(DcrGraphSimple dcr, int round)
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

        #region Utility methods

        private void PrintRelationsInDcrGraphNotInDcrGraphSimple(DcrGraph graph, DcrGraphSimple dcrSimple)
        {
            ErroneouslyRemovedRelations = new HashSet<RedundantRelationEvent>();

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
                            ErroneouslyRemovedRelations.Add(GetRelationInAllResults(source, inclExclTarget, new List<RelationType>{ RelationType.Inclusion, RelationType.Exclusion }));
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
                            ErroneouslyRemovedRelations.Add(GetRelationInAllResults(source, responseTarget, new List<RelationType> { RelationType.Response }));
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
                            ErroneouslyRemovedRelations.Add(GetRelationInAllResults(source, conditionTarget, new List<RelationType> { RelationType.Condition }));
                            Console.WriteLine($"ERROR --> Response from {source.Id} to {conditionTarget.Id} removed faultily.");
                        }
                    }
                }
            }
        }

        private RedundantRelationEvent GetRelationInAllResults(Activity source, Activity target, List<RelationType> relationsWanted)
        {
            return AllResults.Where(x => x is RedundantRelationEvent).Cast<RedundantRelationEvent>()
                .First(x => x.From.Id == source.Id && x.To.Id == target.Id && relationsWanted.Contains(x.Type));
        }

        #endregion
    }
}
