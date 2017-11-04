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
using UlrikHovsgaardAlgorithm.Mining;
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
            return $"{Type} from {From.ToDcrFormatString()} to {To.ToDcrFormatString()} by {Pattern}";
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

        public override string ToString()
        {
            return $"Removal of activity: {Activity.Id} by {Pattern}";
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
        public HashSet<RedundantRelationEvent> RelationsRemovedButNotByCompleteApproach { get; private set; } = new HashSet<RedundantRelationEvent>();
        public (RedundancyEvent, DcrGraphSimple)? CriticalErrorEventWithContext { get; private set; }
        public List<RedundancyEvent> AllResults { get; private set; } = new List<RedundancyEvent>();
        public int RoundsSpent { get; private set; }
        public TimeSpan? TimeSpentCompleteRedundancyRemover { get; private set; }

        public DcrGraphSimple InitialGraph { get; private set; }
        public DcrGraphSimple FinalPatternGraph { get; private set; }
        public DcrGraph FinalCompleteGraph { get; private set; }
        public DcrGraph PatternResultFullyRedundancyRemoved { get; private set; }
        public HashSet<Relation> PatternResultFullyRedundancyRemovedMissingRelations { get; private set; }

        public DcrGraphSimple ApplyEvents(List<RedundancyEvent> events)
        {
            var graph = InitialGraph.Copy();
            foreach (var redundancyEvent in events)
            {
                ApplyEventOnGraph(graph, redundancyEvent);
            }
            return graph;
        }

        private static void ApplyEventsOnGraph(DcrGraphSimple graph, List<RedundancyEvent> events)
        {
            foreach (var ev in events)
            {
                ApplyEventOnGraph(graph, ev);
            }
        }

        private static void ApplyEventOnGraph(DcrGraphSimple graph, RedundancyEvent redundancyEvent)
        {
            switch (redundancyEvent)
            {
                case RedundantActivityEvent ract:
                    graph.MakeActivityDisappear(ract.Activity);
                    break;
                case RedundantRelationEvent rr when rr.Type == RelationType.Condition:
                    graph.RemoveCondition(rr.From, rr.To);
                    break;
                case RedundantRelationEvent rr when rr.Type == RelationType.Inclusion:
                    graph.RemoveInclude(rr.From, rr.To);
                    break;
                case RedundantRelationEvent rr when rr.Type == RelationType.Response:
                    graph.RemoveResponse(rr.From, rr.To);
                    break;
                case RedundantRelationEvent rr when rr.Type == RelationType.Exclusion:
                    graph.RemoveExclude(rr.From, rr.To);
                    break;
            }
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

        public IEnumerable<string> PatternNames => AllResults.Select(x => x.Pattern).Distinct();

        public IEnumerable<RedundancyEvent> RemovedByPattern(string pattern)
        {
            return AllResults.Where(x => pattern.Equals(x.Pattern));
        }

        #endregion

        private static HashSet<Func<DcrGraphSimple, int, List<RedundancyEvent>>> _graphWidePatterns;
        private static HashSet<Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>>> _activityPatterns;
        private static bool _staticSetupDone;
        public static void SetupStaticExecution()
        {
            if (_staticSetupDone) return;

            _graphWidePatterns = new HashSet<Func<DcrGraphSimple, int, List<RedundancyEvent>>>
            {
                Patterns.ApplySequentialSingularExecutionLevelsPattern,
            };
            _activityPatterns = new HashSet<Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>>>
            {
                Patterns.ApplyRedundantRelationsFromUnExecutableActivityPattern,
                Patterns.ApplyCondtionedInclusionPattern,
                Patterns.ApplyIncludesWhenAlwaysCommonlyExcludedAndIncludedPattern,
                Patterns.ApplyRedundantResponsePattern,
                Patterns.ApplyRedundantChainInclusionPattern,
                Patterns.ApplyRedundantPrecedencePattern,
                Patterns.ApplyRedundantTransitiveConditionWith3ActivitiesPattern,
                Patterns.ApplyLastConditionHoldsPattern,
                Patterns.ApplyRedundantChainResponsePattern,
            };

            _staticSetupDone = true;
        }

        public struct ComparisonResult
        {
            public int PatternEventCount { get; set; }
            public int CompleteEventCount { get; set; }
            public RedundancyEvent ErrorEvent { get; set; }
            public DcrGraphSimple ErrorGraphContext { get; set; }
        }

        /// <summary>
        /// A function that isolates itself to only executing in order to attain
        /// removal-ratio statistics of the two compared RR-approaches.
        /// </summary>
        /// <param name="dcr">Initial graph to be given to the complete, exponential RR-appraoch.</param>
        /// <param name="dcrSimple">Initial graph to be given to the pattern RR-approach.</param>
        public static ComparisonResult PerformComparisonGetStatistics(DcrGraph dcr, DcrGraphSimple dcrSimple, bool performErrorDetection)
        {
            SetupStaticExecution();

            DcrGraphSimple simpleCopy = performErrorDetection ? dcrSimple.Copy() : null;

            // TODO: Store static map from given graph mapped to ratio of discovered redundancies + errors, etc.

            DcrGraphSimple before;
            var iterations = 0;

            var patternResults = new List<RedundancyEvent>();
            do
            {
                before = dcrSimple.Copy();

                // Update dcrSimple with optimizations (removals of redundancies)
                patternResults.AddRange(Patterns.ApplyBasicRedundancyRemovalLogic(dcrSimple, iterations));

                foreach (var pattern in _graphWidePatterns)
                {
                    var evs = pattern.Invoke(dcrSimple, iterations);
                    ApplyEventsOnGraph(dcrSimple, evs);
                    patternResults.AddRange(evs);
                }

                foreach (var act in dcr.Activities)
                {
                    foreach (var pattern in _activityPatterns)
                    {
                        var evs = pattern.Invoke(dcrSimple, act, iterations);
                        ApplyEventsOnGraph(dcrSimple, evs);
                        patternResults.AddRange(evs);
                    }
                }

                iterations++;
            }
            while (!before.Equals(dcrSimple));

            var redRem = new RedundancyRemover();
            var (redRemGraph, fullEvents) = redRem.RemoveRedundancyInner(dcr, null, dcrSimple);

            var foundByPatternApproach = patternResults.Count;
            var foundByCompleteApproach = redRem.RedundantRelationsFound;
            var ratioFound = foundByPatternApproach / (double)foundByCompleteApproach;

            RedundancyEvent errorEvent = null;
            DcrGraphSimple errorDcr = null;

            if (performErrorDetection)
            {
                var ourComparer = new UniqueTraceFinder(new ByteDcrGraph(dcr.Copy()));
                foreach (var anEvent in patternResults)
                {
                    ApplyEventOnGraph(simpleCopy, anEvent);

                    if (!ourComparer.CompareTraces(new ByteDcrGraph(simpleCopy)))
                    {
                        // Record that one of the redundancy-events created a semantical difference with the original graph:
                        errorEvent = anEvent;
                        errorDcr = simpleCopy;
                        break;
                    }
                }
            }

            return new ComparisonResult
            {
                PatternEventCount = foundByPatternApproach,
                CompleteEventCount = foundByCompleteApproach,
                ErrorEvent = errorEvent,
                ErrorGraphContext = errorDcr
            };
        }

        public void PerformComparison(DcrGraph dcr, DcrGraph dcrRedundancyRemoved = null, BackgroundWorker bgWorker = null)
        {
            // Reset running-time measurements
            PatternStatistics = new Dictionary<string, RedundancyStatistics>();

            // Convert to pattern-application-friendly type (exploiting efficiency of dual-dictionary structure)
            var dcrSimple = DcrGraphExporter.ExportToSimpleDcrGraph(dcr);
            
            // Pattern-application:
            InitialGraph = dcrSimple.Copy();
            var iterations = 0;
            DcrGraphSimple before;

            SetupStaticExecution();

            List<RedundancyEvent> results = new List<RedundancyEvent>();
            do
            {
                before = dcrSimple.Copy();

                // Update dcrSimple with optimizations (removals of redundancies)
                results.AddRange(Patterns.ApplyBasicRedundancyRemovalLogic(dcrSimple, iterations));
                results.AddRange(ApplyPatterns(dcrSimple, iterations, _graphWidePatterns, _activityPatterns));

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
            var beforeComplete = DateTime.Now;

            var rrGraph = dcrRedundancyRemoved ?? completeRemover.RemoveRedundancyInner(dcr, bgWorker, dcrSimple).Item1;
            
            TimeSpentCompleteRedundancyRemover = dcrRedundancyRemoved == null ? (TimeSpan?)null : DateTime.Now - beforeComplete;

            FinalCompleteGraph = rrGraph;
            RedundantRelationsCountActual = dcrRedundancyRemoved == null
                ? completeRemover.RedundantRelationsFound
                : (dcr.GetRelationCount - rrGraph.GetRelationCount);

            var redundantActivitiesCount = completeRemover.RedundantActivitiesFound;

            // Time-measurement results
            Console.WriteLine("-------------------------------------------------------------");
            foreach (var kvPair in PatternStatistics)
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
            PrintRelationsInDcrGraphNotInDcrGraphSimple(rrGraph, dcrSimple); // AKA: Overshot relations

            /* TODO: The final comparison should comprise of a trace-comparsion to see whether the "erroneous" relations removed
               are actually erroneous.*/

            var comparerTraceFinder = new UniqueTraceFinder(new ByteDcrGraph(FinalCompleteGraph));
            var sameLanguage = comparerTraceFinder.CompareTraces(new ByteDcrGraph(dcrSimple));
            var ourTraceFinder = new UniqueTraceFinder(new ByteDcrGraph(dcrSimple));

            var originalComparer = new UniqueTraceFinder(new ByteDcrGraph(InitialGraph.Copy()));
            var originalSameLanguage = originalComparer.CompareTraces(new ByteDcrGraph(dcrSimple));

            var completeOriginalSameLanguage = originalComparer.CompareTraces(new ByteDcrGraph(FinalCompleteGraph));

            var ourCopy = InitialGraph.Copy();
            var ourComparer = new UniqueTraceFinder(new ByteDcrGraph(InitialGraph.Copy()));
            foreach (var anEvent in AllResults)
            {
                ApplyEventOnGraph(ourCopy, anEvent);

                if (!ourComparer.CompareTraces(new ByteDcrGraph(ourCopy)))
                {
                    // Record that one of the redundancy-events created a semantical difference with the original graph:
                    CriticalErrorEventWithContext = (anEvent, ourCopy);

                    Console.WriteLine($"Here is the darned culprit! {anEvent}");
                    Console.WriteLine($"And here be the graph:\n{DcrGraphExporter.ExportToXml(ourCopy)}");
                    break;
                }
            }
            
            var (continued, continuedRelations) = completeRemover.RemoveRedundancyInner(dcrSimple.ToDcrGraph(), bgWorker, ourCopy);
            PatternResultFullyRedundancyRemoved = continued;
            PatternResultFullyRedundancyRemovedMissingRelations = continuedRelations;
            Console.WriteLine($"--> Sanity check: Are 'ourCopy' and 'dcrSimple equal?':::::> {ourCopy.Equals(dcrSimple)}");

            Console.WriteLine($"Is the RR-graphs' language the same?!: {sameLanguage}");
            Console.WriteLine($"Is our RR-graph and the orignal's language the same?!: {originalSameLanguage}");
            Console.WriteLine($"Is the complete RR-graph and the orignal's language the same?!: {completeOriginalSameLanguage}");

            // Export to XML
            //Console.WriteLine("RESULT-DCR GRAPH:");
            //Console.WriteLine(DcrGraphExporter.ExportToXml(dcrSimple));
        }

        /// <summary>
        /// Applies all our great patterns.
        /// </summary>
        /// <returns>Amount of relations removed</returns>
        private List<RedundancyEvent> ApplyPatterns(DcrGraphSimple dcr, int iterations, HashSet<Func<DcrGraphSimple, int, List<RedundancyEvent>>> graphwidePatterns,
            HashSet<Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>>> activityPatterns)
        {
            var events = new List<RedundancyEvent>();

            foreach (var pattern in graphwidePatterns)
            {
                var evs = ExecuteWithStatistics(pattern, dcr, iterations);
                ApplyEventsOnGraph(dcr, evs);
                events.AddRange(evs);
            }

            foreach (var act in dcr.Activities)
            {
                foreach (var pattern in activityPatterns)
                {
                    var evs = ExecuteWithStatistics(pattern, dcr, act, iterations);
                    ApplyEventsOnGraph(dcr, evs);
                    events.AddRange(evs);
                }
                //set.Add(ExecuteWithStatistics(ApplyRedundantIncludeWhenIncludeConditionExistsPattern, dcr, act,
                //  iterations));
            }

            return events;
        }

        private readonly bool _measureRunningTimes = true;

        public class RedundancyStatistics
        {
            /// <summary>
            /// The amount of redundant activities and/or relations removed.
            /// </summary>
            public int RedundancyCount { get; set; }
            /// <summary>
            /// The combined time spent.
            /// </summary>
            public TimeSpan TimeSpent { get; set; }
        }

        /// <summary>
        /// Maps from a pattern-name to the amount of redudancy-events discovered by it as an integer,
        /// and the time spent executing the pattern as a TimeSpan.
        /// </summary>
        public Dictionary<string, RedundancyStatistics> PatternStatistics = new Dictionary<string, RedundancyStatistics>();

        private List<RedundancyEvent> ExecuteWithStatistics(Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>> func, DcrGraphSimple dcr, Activity act, int round)
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
                PatternStatistics.Add(func.Method.Name, new RedundancyStatistics { RedundancyCount = result.Count, TimeSpent = end - start});
            //Console.WriteLine($"{func.Method.Name} took {end - start:g}");

            return result;
        }

        private List<RedundancyEvent> ExecuteWithStatistics(Func<DcrGraphSimple, int, List<RedundancyEvent>> func, DcrGraphSimple dcr, int round)
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
                PatternStatistics.Add(func.Method.Name, new RedundancyStatistics { RedundancyCount = result.Count, TimeSpent = end - start });
            //Console.WriteLine($"{func.Method.Name} took {end - start:g}");

            return result;
        }
        
        #region Utility methods

        private void PrintRelationsInDcrGraphNotInDcrGraphSimple(DcrGraph graph, DcrGraphSimple dcrSimple)
        {
            RelationsRemovedButNotByCompleteApproach = new HashSet<RedundantRelationEvent>();

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
                            RelationsRemovedButNotByCompleteApproach.Add(GetRelationInAllResults(source, inclExclTarget, new List<RelationType>{ RelationType.Inclusion, RelationType.Exclusion }));
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
                            RelationsRemovedButNotByCompleteApproach.Add(GetRelationInAllResults(source, responseTarget, new List<RelationType> { RelationType.Response }));
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
                            RelationsRemovedButNotByCompleteApproach.Add(GetRelationInAllResults(source, conditionTarget, new List<RelationType> { RelationType.Condition }));
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
