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

        private void ApplyEventsOnGraph(DcrGraphSimple graph, List<RedundancyEvent> events)
        {
            foreach (var ev in events)
            {
                ApplyEventOnGraph(graph, ev);
            }
        }

        private void ApplyEventOnGraph(DcrGraphSimple graph, RedundancyEvent redundancyEvent)
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

        public IEnumerable<string> Patterns => AllResults.Select(x => x.Pattern).Distinct();

        public IEnumerable<RedundancyEvent> RemovedByPattern(string pattern)
        {
            return AllResults.Where(x => pattern.Equals(x.Pattern));
        }

        #endregion

        public void PerformComparison(DcrGraph dcr, DcrGraph dcrRedundancyRemoved = null, BackgroundWorker bgWorker = null)
        {
            // Reset running-time measurements
            MethodRunningTimes = new Dictionary<string, TimeSpan>();

            // Convert to pattern-application-friendly type (exploiting efficiency of dual-dictionary structure)
            var dcrSimple = DcrGraphExporter.ExportToSimpleDcrGraph(dcr);
            
            // Pattern-application:
            InitialGraph = dcrSimple.Copy();
            var iterations = 0;
            DcrGraphSimple before;
            var graphwidePatterns = new HashSet<Func<DcrGraphSimple, int, List<RedundancyEvent>>>
            {
                ApplySequentialSingularExecutionLevelsPattern,
            };
            var activityPatterns = new HashSet<Func<DcrGraphSimple, Activity, int, List<RedundancyEvent>>>
            {
                ApplyRedundantRelationsFromUnExecutableActivityPattern,
                ApplyCondtionedInclusionPattern,
                ApplyIncludesWhenAlwaysCommonlyExcludedAndIncludedPattern,
                ApplyRedundantResponsePattern,
                ApplyRedundantChainInclusionPattern,
                ApplyRedundantPrecedencePattern,
                ApplyRedundantTransitiveConditionWith3ActivitiesPattern,
                ApplyLastConditionHoldsPattern,
                ApplyRedundantChainResponsePattern,
            };
            List<RedundancyEvent> results = new List<RedundancyEvent>();
            do
            {
                before = dcrSimple.Copy();
                // Basic-optimize simple-graph:
                results.AddRange(ApplyBasicRedundancyRemovalLogic(dcrSimple, iterations));
                results.AddRange(ApplyPatterns(dcrSimple, iterations, graphwidePatterns, activityPatterns));
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
            foreach (var kvPair in MethodRunningTimes)
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
        public Dictionary<string, TimeSpan> MethodRunningTimes = new Dictionary<string, TimeSpan>();
        private T ExecuteWithStatistics<T>(Func<DcrGraphSimple, Activity, int, T> func, DcrGraphSimple dcr, Activity act, int round)
        {
            if (!_measureRunningTimes)
                return func.Invoke(dcr, act, round);

            // Else: Perform running-time measurements and store them with the invoked method's name
            var start = DateTime.Now;
            var tResult = func.Invoke(dcr, act, round);
            var end = DateTime.Now;

            // Add the running time to the combined running time for this pattern-search method
            if (MethodRunningTimes.TryGetValue(func.Method.Name, out var runningTime))
                MethodRunningTimes[func.Method.Name] = runningTime.Add(end - start);
            else
                MethodRunningTimes.Add(func.Method.Name, end - start);
            //Console.WriteLine($"{func.Method.Name} took {end - start:g}");

            return tResult;
        }

        private T ExecuteWithStatistics<T>(Func<DcrGraphSimple, int, T> func, DcrGraphSimple dcr, int round)
        {
            if (!_measureRunningTimes)
                return func.Invoke(dcr, round);

            // Else: Perform running-time measurements and store them with the invoked method's name
            var start = DateTime.Now;
            var tResult = func.Invoke(dcr, round);
            var end = DateTime.Now;

            // Add the running time to the combined running time for this pattern-search method
            if (MethodRunningTimes.TryGetValue(func.Method.Name, out var runningTime))
                MethodRunningTimes[func.Method.Name] = runningTime.Add(end - start);
            else
                MethodRunningTimes.Add(func.Method.Name, end - start);
            //Console.WriteLine($"{func.Method.Name} took {end - start:g}");

            return tResult;
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
        private List<RedundancyEvent> ApplyCondtionedInclusionPattern(DcrGraphSimple dcr, Activity A, int round)
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

        private bool hasChainConditionTo(Activity from, Activity to, DcrGraphSimple dcr, HashSet<Activity> seenBefore)
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
        private List<RedundancyEvent> ApplyRedundantResponsePattern(DcrGraphSimple dcr, Activity act, int round)
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
        private List<RedundancyEvent> ApplyIncludesWhenAlwaysCommonlyExcludedAndIncludedPattern(DcrGraphSimple dcr, Activity act, int round)
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
        private List<RedundancyEvent> ApplySequentialSingularExecutionLevelsPattern(DcrGraphSimple dcr, int round)
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
        private List<RedundancyEvent> ApplyRedundantTransitiveConditionWith3ActivitiesPattern(DcrGraphSimple dcr, Activity C, int round)
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
                    if (A.IncludesMe(dcr).Except(new List<Activity> {B, C}).Any()
                        && A.ExcludesMe(dcr).Except(new List<Activity> {B, C}).Any())
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
                        Equals(inclSourceB, C) || Chase(dcr, inclSourceB, C.IncludesMe(dcr), 4));
                }
                if (canDo)
                {
                    events.Add(new RedundantRelationEvent(patternName, RelationType.Inclusion, B, C, round));
                }
                
            }

            return events;
        }

        private Boolean Chase(DcrGraphSimple dcr, Activity A, HashSet<Activity> mustInclude, int countdown)
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

        private List<RedundancyEvent> ApplyRedundantChainResponsePattern(DcrGraphSimple dcr, Activity C, int round)
        {
            var events = new List<RedundancyEvent>();
            var pattern = "RedundantChainResponsePattern";
            
            foreach (var A in C.ResponsesMe(dcr))
            {
                if (ResponseChase(dcr,  null, A, C, 4))
                {
                    events.Add(new RedundantRelationEvent(pattern, RelationType.Response, A, C, round));
                }
            }

            return events;
        }

        private Boolean ResponseChase(DcrGraphSimple dcr, Activity previous, Activity current, Activity target, int countdown)
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
        private List<RedundancyEvent> ApplyRedundantRelationsFromUnExecutableActivityPattern(DcrGraphSimple dcr, Activity act, int round)
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
