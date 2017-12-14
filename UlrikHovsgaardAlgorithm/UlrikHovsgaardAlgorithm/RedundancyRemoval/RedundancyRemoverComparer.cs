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
    public static class RedundancyRemoverComparer
    {
        public static int GlobalPatternNotSameLanguageAsCompleteCounter { get; private set; }

        public static DcrGraphSimple ApplyEventsAsNewGraph(DcrGraphSimple graph, List<RedundancyEvent> events)
        {
            var copy = graph.Copy();

            foreach (var ev in events)
            {
                ApplyEventOnGraph(copy, ev);
            }

            return copy;
        }

        public static void ApplyEventsOnGraph(DcrGraphSimple graph, List<RedundancyEvent> events)
        {
            foreach (var ev in events)
            {
                ApplyEventOnGraph(graph, ev);
            }
        }

        public static void ApplyEventOnGraph(DcrGraphSimple graph, RedundancyEvent redundancyEvent)
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

        public static List<RedundancyEvent> GetEventsUpUntil(List<RedundancyEvent> allEvents, RedundancyEvent untilEvent)
        {
            var index = allEvents.FindIndex(x => x == untilEvent);
            return allEvents.GetRange(0, index); // All events excluding the given event
        }

        public static DcrGraphSimple GetContextBeforeEvent(DcrGraphSimple initial, List<RedundancyEvent> allEvents, RedundancyEvent contextEvent)
        {
            return ApplyEventsAsNewGraph(initial, GetEventsUpUntil(allEvents, contextEvent));
        }

        public static readonly HashSet<Func<DcrGraphSimple, int, List<RedundancyEvent>>> GraphWidePatterns
            = new HashSet<Func<DcrGraphSimple, int, List<RedundancyEvent>>>
            {
                Patterns.ApplyBasicRedundancyRemovalLogic,
                Patterns.ApplySequentialSingularExecutionPattern,
            };

        public static readonly HashSet<Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>>> ActivityPatterns
            = new HashSet<Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>>>
            {
                Patterns.ApplyRedundantRelationsFromUnExecutableActivityPattern,
                Patterns.ApplyConditionedInclusionPattern,
                Patterns.ApplyIncludesWhenAlwaysCommonlyExcludedAndIncludedPattern,
                Patterns.ApplyRedundantChainInclusionPattern,
                Patterns.ApplyRedundantPrecedencePattern,
                Patterns.ApplyRedundantTransitiveConditionWith3ActivitiesPattern,
                Patterns.ApplyLastConditionHoldsPattern,
                //Patterns.ApplyRedundantChainResponsePattern,
            };

        public class ComparisonResult
        {
            // GRAPHS INVOLVED
            public DcrGraphSimple InitialGraph { get; set; }
            public DcrGraphSimple PatternApproachResult { get; set; }
            public DcrGraph CompleteApproachResult { get; set; }

            public int PatternRelationsRemovedCount { get; set; }
            public int CompleteRelationsRemovedCount { get; set; }

            public List<string> ErrorTrace { get; set; }

            public int PatternApproachRoundsSpent { get; set; }

            public bool ErrorOccurred => ErrorEvent != null || ErrorGraphContext != null;

            /// <summary>
            /// Optional property in case the comparison had an erroneous redundancy-removal.
            /// If so, this is the event that caused the error.
            /// </summary>
            public RedundancyEvent ErrorEvent { get; set; }

            /// <summary>
            /// The graph right after applying the "ErrorEvent".
            /// </summary>
            public DcrGraphSimple ErrorGraphContext { get; set; }

            public List<RedundancyEvent> EventsByPatternApproach { get; set; }

            public Dictionary<string, (List<RedundancyEvent>, TimeSpan)> PatternStatistics { get; set; } = new Dictionary<string, (List<RedundancyEvent>, TimeSpan)>();

            public TimeSpan PatternApproachTimeSpent => PatternStatistics?.Values.Select(v => v.Item2).Aggregate((v1, v2) => v1 + v2) ?? default(TimeSpan);
            public TimeSpan CompleteApproachTimeSpent { get; set; }

            public HashSet<RedundantRelationEvent> RelationsRemovedByPatternNotByCompleteApproach { get; set; } = new HashSet<RedundantRelationEvent>();

            public DcrGraph PatternResultFullyRedundancyRemoved { get; set; }
            public HashSet<Relation> PatternResultFullyRedundancyRemovedRelationsRemoved { get; set; } = new HashSet<Relation>();
        }

        public static PatternAlgorithmResult ApplyPatternAlgorithm(DcrGraphSimple dcr)
        {
            var statistics = new Dictionary<string, (List<RedundancyEvent>, TimeSpan)>();
            var iterations = 0;
            var actualEventList = new List<RedundancyEvent>();
            DcrGraphSimple before;

            do
            {
                before = dcr.Copy();

                // Update dcrSimple with optimizations (removals of redundancies)
                var (patternStatistics, eventList) = Patterns.ApplyPatterns(dcr, iterations, GraphWidePatterns, ActivityPatterns);
                statistics.UpdateWith(patternStatistics);
                actualEventList.AddRange(eventList);
                iterations++;
            }
            while (!before.Equals(dcr));

            return new PatternAlgorithmResult
            {
                Redundancies = actualEventList,//statistics.Values.SelectMany(v => v.Item1).ToList(),
                PatternStatistics = statistics,
                RoundsSpent = iterations,
            };
        }

        /// <summary>
        /// A function that isolates itself to only executing in order to attain
        /// removal-ratio statistics of the two compared RR-approaches.
        /// </summary>
        /// <param name="dcr">Initial graph to be given to the complete, exponential RR-appraoch.</param>
        /// <param name="dcrSimple">Initial graph to be given to the pattern RR-approach.</param>
        public static ComparisonResult PerformComparisonClean(DcrGraph dcr, DcrGraphSimple dcrSimple)
        {
            var before = DateTime.Now;
            var patternAlgResult = ApplyPatternAlgorithm(dcrSimple);
            var patternTimeSpent = DateTime.Now - before;

            var redRem = new RedundancyRemover();
            before = DateTime.Now;
            var (redRemGraph, fullEvents) = redRem.RemoveRedundancyInner(dcr); // We just measure time and redundancy-count here
            var completeTimeSpent = DateTime.Now - before;
            
            return new ComparisonResult
            {
                PatternApproachResult = dcrSimple,
                CompleteApproachResult = redRemGraph,
                PatternRelationsRemovedCount = patternAlgResult.Redundancies.Count,
                CompleteRelationsRemovedCount = redRem.RedundantRelationsFound,
                EventsByPatternApproach = patternAlgResult.Redundancies,
                PatternStatistics = patternAlgResult.PatternStatistics,
                PatternApproachRoundsSpent = patternAlgResult.RoundsSpent,
                CompleteApproachTimeSpent = completeTimeSpent,
            };
        }

        public static ComparisonResult PerformComparisonWithPostEvaluation(DcrGraph dcr, DcrGraph dcrRedundancyRemoved = null, BackgroundWorker bgWorker = null)
        {
            // Convert to pattern-application-friendly type (exploiting efficiency of dual-dictionary structure)
            var dcrSimple = DcrGraphExporter.ExportToSimpleDcrGraph(dcr);
            var byteDcrFormat = new ByteDcrGraph(dcrSimple, null);

            // Pattern-application:
            var initialGraph = dcrSimple.Copy();
            var asRegularGraph = initialGraph.ToDcrGraph();

            if (dcr.IncludesCount != asRegularGraph.IncludesCount || dcr.ExcludesCount != asRegularGraph.ExcludesCount
                || dcr.ResponsesCount != asRegularGraph.ResponsesCount || dcr.ConditionsCount != asRegularGraph.ConditionsCount
                || dcr.Activities.Count != asRegularGraph.Activities.Count
                // Or it holds for an activity in dcr that no activity in "asRegularGraph" is equal to it, both name, id and state!:
                || dcr.Activities.Any(a => !asRegularGraph.Activities.Any(o => a.IsSameNameIdAndStateAsOther(o))))
            {
                int i = 0;
            }

            var patternAlgResult = ApplyPatternAlgorithm(dcrSimple);

            // Apply complete redundancy-remover and print when relations are redundant, that were not also removed in the Simple result.:
            var completeRemover = new RedundancyRemover();

            var beforeComplete = DateTime.Now;
            var finalCompleteGraph = dcrRedundancyRemoved ?? completeRemover.RemoveRedundancyInner(asRegularGraph, byteDcrFormat, bgWorker, dcrSimple).Item1;
            var timeSpentCompleteRedundancyRemover = dcrRedundancyRemoved != null ? default(TimeSpan) : DateTime.Now - beforeComplete;

            var patternRedundantRelationsCount = patternAlgResult.Redundancies.Count(r => r is RedundantRelationEvent);
            var patternApproachRelationCountDifference = initialGraph.RelationsCount - dcrSimple.RelationsCount;
            if (patternRedundantRelationsCount != patternApproachRelationCountDifference)
            {
                Console.WriteLine("--> Still bad at counting.");
            }

            var completeRedundantRelationsCount = dcrRedundancyRemoved == null
                ? completeRemover.RedundantRelationsFound
                : asRegularGraph.GetRelationCount - finalCompleteGraph.GetRelationCount;
            
            var completeRedundantActivitiesCount = completeRemover.RedundantActivitiesFound;
            
            // Check for, and inform about relations removed by pattern-approach, but not the complete redudancy-remover
            var removedByPatternNotByComplete = PrintRelationsInDcrGraphNotInDcrGraphSimple(finalCompleteGraph, dcrSimple, patternAlgResult.Redundancies); // AKA: "Overshot removals"
            
            // DETECTION OF POTENTIAL ERROR:
            var (errorEvent, errorEventContext, errorTrace) = FindErrorByApplyingEvents(initialGraph.Copy(), patternAlgResult.Redundancies);
            
            // Finishing redundancy-removal on the pattern-approach result (Seeing what was missed):
            var (continued, continuedRelations) = new RedundancyRemover().RemoveRedundancyInner(dcrSimple.ToDcrGraph(), byteDcrFormat, bgWorker, initialGraph.Copy());

            // SANITY CHECK: Comparing language of pattern-result and Complete approach results:
            var sanityChecker = new UniqueTraceFinder(new ByteDcrGraph(dcrSimple, byteDcrFormat));
            if (errorEvent == null && !sanityChecker.CompareTraces(new ByteDcrGraph(finalCompleteGraph, byteDcrFormat)))
            {
                // Let's check further - WHO doesn't conform to the original?
                var sanityCheckerInner = new UniqueTraceFinder(new ByteDcrGraph(initialGraph.Copy(), byteDcrFormat));
                var patternSameLanguage = sanityCheckerInner.CompareTraces(new ByteDcrGraph(dcrSimple, byteDcrFormat));
                var completeSameLanguage = sanityCheckerInner.CompareTraces(new ByteDcrGraph(finalCompleteGraph, byteDcrFormat));

                var completeXml = DcrGraphExporter.ExportToXml(finalCompleteGraph);
                var patternXml = DcrGraphExporter.ExportToXml(dcrSimple);

                GlobalPatternNotSameLanguageAsCompleteCounter++;

                //Console.WriteLine("Dang! Language of pattern result and Complete result apparently unequal!");
            }

            if (patternApproachRelationCountDifference > completeRedundantRelationsCount)
            {
                int i = 0;
            }

            return new ComparisonResult
            {
                InitialGraph = initialGraph,
                PatternApproachResult = dcrSimple,
                CompleteApproachResult = finalCompleteGraph,
                PatternRelationsRemovedCount = patternApproachRelationCountDifference,
                CompleteRelationsRemovedCount = completeRedundantRelationsCount,
                PatternApproachRoundsSpent = patternAlgResult.RoundsSpent,
                ErrorEvent = errorEvent,
                ErrorGraphContext = errorEventContext,
                EventsByPatternApproach = patternAlgResult.Redundancies,
                PatternStatistics = patternAlgResult.PatternStatistics,
                CompleteApproachTimeSpent = timeSpentCompleteRedundancyRemover,
                RelationsRemovedByPatternNotByCompleteApproach = removedByPatternNotByComplete,
                PatternResultFullyRedundancyRemoved = continued,
                PatternResultFullyRedundancyRemovedRelationsRemoved = continuedRelations,
                ErrorTrace = errorTrace
            };
        }

        public static (RedundancyEvent, DcrGraphSimple, List<string>) FindErrorByApplyingEvents(DcrGraphSimple initialGraph, List<RedundancyEvent> events)
        {
            RedundancyEvent errorEvent = null;
            DcrGraphSimple errorEventContext = null;
            List<string> errorTrace = null;
            var ourCopy = initialGraph.Copy();
            var initialByteDcr = new ByteDcrGraph(initialGraph.Copy(), null);
            var ourComparer = new UniqueTraceFinder(new ByteDcrGraph(ourCopy, initialByteDcr));
            DcrGraphSimple prevGraph = ourCopy.Copy();
            foreach (var anEvent in events)
            {
                ApplyEventOnGraph(ourCopy, anEvent);

                if (!ourComparer.CompareTraces(new ByteDcrGraph(ourCopy, initialByteDcr)))
                {
                    // Record that one of the redundancy-events created a semantical difference with the original graph:
                    errorEvent = anEvent;
                    errorEventContext = prevGraph;
                    errorTrace = ourComparer.ComparisonFailureTrace;
                    break;
                }

                // When an error occurs, we wish to see the graph before that event is removed :)
                prevGraph = ourCopy.Copy();
            }

            return (errorEvent, errorEventContext, errorTrace);
        }
        
        #region Utility methods

        private static HashSet<RedundantRelationEvent> PrintRelationsInDcrGraphNotInDcrGraphSimple(DcrGraph graph, DcrGraphSimple dcrSimple, List<RedundancyEvent> eventsToSearch)
        {
            var res = new HashSet<RedundantRelationEvent>();

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
                            res.Add(GetRelationInAllResults(eventsToSearch, source, inclExclTarget,
                                new List<RelationType> {RelationType.Inclusion, RelationType.Exclusion}));
                            //Console.WriteLine($"ERROR --> Include/Exclude from {source.Id} to {inclExclTarget.Id} removed faultily.");
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
                            res.Add(GetRelationInAllResults(eventsToSearch, source, responseTarget,
                                new List<RelationType> {RelationType.Response}));
                            //Console.WriteLine($"ERROR --> Response from {source.Id} to {responseTarget.Id} removed faultily.");
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
                            res.Add(GetRelationInAllResults(eventsToSearch, source, conditionTarget,
                                new List<RelationType> {RelationType.Condition}));
                            //Console.WriteLine($"ERROR --> Response from {source.Id} to {conditionTarget.Id} removed faultily.");
                        }
                    }
                }
            }

            return res;
        }

        private static RedundantRelationEvent GetRelationInAllResults(List<RedundancyEvent> eventsToSearch, Activity source, Activity target, List<RelationType> relationsWanted)
        {
            return eventsToSearch.Where(x => x is RedundantRelationEvent).Cast<RedundantRelationEvent>()
                .First(x => x.From.Id == source.Id && x.To.Id == target.Id && relationsWanted.Contains(x.Type));
        }

        #endregion
    }
}
