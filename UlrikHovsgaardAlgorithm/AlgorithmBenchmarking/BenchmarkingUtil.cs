using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.QualityMeasures;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardAlgorithm.Utils;

namespace AlgorithmBenchmarking
{
    public class BenchmarkingUtil
    {
        private static int LanguageDifferenceCountPreMined;
        private static int LanguageDifferenceCountInclAll;
        private const string OUTPUT_FILE = "C:\\RedundancyOutputs\\log.txt";

        public static void Main(string[] args)
        {
            if (File.Exists(OUTPUT_FILE))
            {
                Console.WriteLine("log.txt exists!!!!");
                return;
            }
            const bool doSelfConditions = true;
            const int activitiesMax = 8;
            const int activityToRelationRatio = 5;
            const int relationsMax = activitiesMax*activityToRelationRatio;
            const int numberOfGraphs = 1000;
            var graphs = GraphGenerator.Generate(activitiesMax, relationsMax, numberOfGraphs, g =>
            {
                var activitiesCopy = new List<Activity>();
                foreach (var act in g.Activities)
                {
                    var newAct = new Activity(act.Id, act.Name);
                    newAct.Included = act.Included;
                    newAct.Pending = act.Pending;
                    newAct.Executed = act.Executed;
                    activitiesCopy.Add(newAct);
                }
                var remover = new RedundancyRemover();
                var other = g.ToDcrGraph();
                if (other.GetRelationCount != relationsMax)
                    Console.WriteLine("Stuff is off");
                Console.WriteLine("Removing redundancy...");
                var unique = new UniqueTraceFinder(new ByteDcrGraph(g, null));
                
                // Should allow SOME language:
                if (unique.IsNoAcceptingTrace())
                    return false;

                var (finalGraph, res) = remover.RemoveRedundancyInner(other);
                
                // Should contain some redundancies:
                return remover.RedundantActivitiesFound > 0 || remover.RedundantRelationsFound > 0;
            }, doSelfConditions);
            Console.WriteLine("Running tests");
            
            

            Console.WriteLine("Ran tests");
            
            var home = "C:\\RedundancyOutputs";
            var minedGraphs = new List<DcrGraph>();
            var generatedResults = new List<RedundancyRemoverComparer.ComparisonResult>();
            var minedResults = new List<RedundancyRemoverComparer.ComparisonResult>();
            RedundancyRemoverComparer.ComparisonResult genGraphsResults = null;
            RedundancyRemoverComparer.ComparisonResult minedGraphsResults = null;
            using (var csv = new StreamWriter(home + "\\dcr_results_generated.csv"))
            {
                int counter = 0;
                foreach (var g in graphs)
                {
                    /*
                    if (counter == 2)
                    {
                        int j = 2;
                        var theRealOne = graphs.Where(x =>
                            x.Activities.First(y => y.Id == "a5").Pending &&
                            !x.Activities.First(y => y.Id == "a5").Included);
                        int hh = 2;
                        var less = theRealOne.Where(x =>
                            x.Activities.First(y => y.Id == "a5")
                                .HasConditionTo(x.Activities.First(y => y.Id == "a5"), x));
                        int sdasda = 2;
                        var first = less.First();
                        int sadasdasdasd = 2;
                    }
                    */
                    generatedResults.Add(RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g.ToDcrGraph()));
                    counter++;
                }

                LanguageDifferenceCountPreMined = RedundancyRemoverComparer.GlobalPatternNotSameLanguageAsCompleteCounter;

                //var results = graphs.Select(g => RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g.ToDcrGraph()));
                genGraphsResults = WriteResults(generatedResults, csv, home, "generated");

                foreach (var res in generatedResults)
                {
                    //var logGenerator = new LogGenerator9001(75, res.InitialGraph.ToDcrGraph());
                    //var traces = logGenerator.GenerateLog(500);
                    //if (traces == null)
                    //    continue;

                    // Build a log from the initial graph's language:
                    var traceFinderInitialGraph = new UniqueTraceFinder(new ByteDcrGraph(res.InitialGraph.Copy(), null));
                    var eventId = 0;
                    var traces = traceFinderInitialGraph.GetLanguageAsListOfTracesWithIds().Select(listOfIds =>
                    {
                        var trace = new LogTrace { Id = Guid.NewGuid().ToString() };
                        listOfIds.ForEach(id => trace.Add(new LogEvent(id, id) { EventId = eventId++.ToString() }));
                        trace.IsFinished = true;
                        return trace;
                    }).ToList();

                    // Perform process mining:
                    var miner = new ContradictionApproach(res.InitialGraph.Activities);
                    foreach (var trace in traces)
                    {
                        miner.AddTrace(trace);
                    }
                    
                    minedGraphs.Add(miner.Graph);
                }
            }

            Console.WriteLine("Beginning mining approach...");
            using (var csv = new StreamWriter(home + "\\dcr_results_mined.csv"))
            {
                Console.WriteLine($"Number of mined graphs: {minedGraphs.Count}");
                minedResults = minedGraphs.Select(g => RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g)).ToList();
                minedGraphsResults = WriteResults(minedResults, csv, home, "mined");
            }

            LanguageDifferenceCountInclAll = RedundancyRemoverComparer.GlobalPatternNotSameLanguageAsCompleteCounter;

            var generatedCompleteCount = genGraphsResults.CompleteRelationsRemovedCount;
            var generatedPatternCount = genGraphsResults.PatternRelationsRemovedCount;
            var minedCompleteCount = minedGraphsResults.CompleteRelationsRemovedCount;
            var minedPatternsCount = minedGraphsResults.PatternRelationsRemovedCount;

            var fileOut = new FileStream(OUTPUT_FILE, FileMode.Create, FileAccess.Write);
            var streamWriter = new StreamWriter(fileOut);
            Console.SetOut(streamWriter);

            // Maybe ignore this print:
            Console.WriteLine($"Language difference between Patter and Complete approach: Before Mined graphs: {LanguageDifferenceCountPreMined}, After: {LanguageDifferenceCountInclAll}");

            Console.WriteLine("---------------------------------------");
            
            //    PatternResultFullyRedundancyRemovedRelationsRemoved = redundanciesLeftAfterPatternApproachCombined

            PrintPatternStatistics("GENERATED", genGraphsResults.PatternStatistics);
            PrintPatternStatistics("PROCESS-MINED", minedGraphsResults.PatternStatistics);

            Console.WriteLine($"Amount of relations removed by pattern approach, not by complete (GENERATED): {genGraphsResults.RelationsRemovedByPatternNotByCompleteApproach.Count}");
            Console.WriteLine($"Amount of relations removed by pattern approach, not by complete (PROCESS-MINED): {minedGraphsResults.RelationsRemovedByPatternNotByCompleteApproach.Count}");
            
            Console.WriteLine($"Pattern-approach average rounds spent (GENERATED): {genGraphsResults.PatternApproachRoundsSpent / (double)generatedResults.Count}");
            Console.WriteLine($"Pattern-approach average rounds spent (PROCESS-MINED): {minedGraphsResults.PatternApproachRoundsSpent / (double)minedResults.Count}");
            
            Console.WriteLine($"Total time spent by complete approach (GENERATED): {genGraphsResults.CompleteApproachTimeSpent}");
            Console.WriteLine($"Total time spent by complete approach (PROCESS-MINED): {minedGraphsResults.CompleteApproachTimeSpent}");

            Console.WriteLine($"Total time spent by pattern approach (GENERATED): {genGraphsResults.PatternApproachTimeSpent}");
            Console.WriteLine($"Total time spent by pattern approach (PROCESS-MINED): {minedGraphsResults.PatternApproachTimeSpent}");

            Console.WriteLine($"Redundant relations left, after pattern approach (GENERATED): {genGraphsResults.PatternResultFullyRedundancyRemovedRelationsRemoved.Count}");
            Console.WriteLine($"Redundant relations left, after pattern approach (PROCESS-MINED): {minedGraphsResults.PatternResultFullyRedundancyRemovedRelationsRemoved.Count}");

            var percentage = generatedCompleteCount == 0 ? 1.0 : (double)generatedPatternCount / generatedCompleteCount;
            Console.WriteLine($"Generated approach: Pattern / Complete = {percentage} ({generatedPatternCount} / {generatedCompleteCount})");
            percentage = minedCompleteCount == 0 ? 1.0 : (double)minedPatternsCount / minedCompleteCount;
            Console.WriteLine($"Mined approach: Pattern / Complete = {percentage} ({minedPatternsCount} / {minedCompleteCount})");
            Console.WriteLine($"Generated number of traces: {genGraphsResults.NumberOfTraces}");
            Console.WriteLine($"Generated average trace length: {genGraphsResults.AverageTraceLength}");
            Console.WriteLine($"Mined number of traces: {minedGraphsResults.NumberOfTraces}");
            Console.WriteLine($"Mined average trace length: {minedGraphsResults.AverageTraceLength}");
            Console.WriteLine("DONE!");
            Console.Read();
            streamWriter.Close();
            fileOut.Close();
        }

        private static void PrintPatternStatistics(string group, Dictionary<string, (List<RedundancyEvent>, TimeSpan)> patternStatistics)
        {
            Console.WriteLine($"Pattern-stats for {group} graphs:");
            foreach (var kv in patternStatistics)
            {
                Console.WriteLine($"{kv.Key}: Found {kv.Value.Item1.Count}, took {kv.Value.Item2}");
            }
        }

        public static RedundancyRemoverComparer.ComparisonResult WriteResults(IEnumerable<RedundancyRemoverComparer.ComparisonResult> results, StreamWriter csv, string home, string errorPrefix)
        {
            int errorCount = 0;
            int completeApproach = 0;
            int patternApproach = 0;
            int counter = 0;
            Dictionary<string, (List<RedundancyEvent>, TimeSpan)> combinedPatternStatistics = new Dictionary<string, (List<RedundancyEvent>, TimeSpan)>();
            var relationsRemovedByPatternsNotByCompleteSum = new HashSet<RedundantRelationEvent>();
            var roundsSpentPatternApproachSum = 0;
            var timeSpentCompleteApproach = new TimeSpan(); // Don't need to manage time spent on pattern approach - getter-property in ComparisonResult
            var redundanciesLeftAfterPatternApproachCombined = new HashSet<Relation>();
            int numberOfTraces = 0;
            int traceLengthSum = 0;
            int successCount = 0;
           

            foreach (var res in results)
            {                
                Console.WriteLine("----------------------");
                if (res.ErrorOccurred)
                {
                    Console.WriteLine("ERROR!");
                }
                else
                {
                    completeApproach += res.CompleteRelationsRemovedCount;
                    patternApproach += res.PatternRelationsRemovedCount;
                }
                Console.WriteLine($"Original RelationCount: {res.InitialGraph.RelationsCount}");
                Console.WriteLine($"Pattern approach redundancy events: {res.PatternRelationsRemovedCount}");
                Console.WriteLine($"Complete approach redundancy events: {res.CompleteRelationsRemovedCount}");
                Console.WriteLine($"Pattern / Complete = {(res.CompleteRelationsRemovedCount == 0 ? 100.0 : (res.PatternRelationsRemovedCount + 0.0) / (res.CompleteRelationsRemovedCount + 0.0))}");
                // pattern: hash, number of events, number of relations, number of redundant relations per full, number of redundant events per full, number of redundant relations per pattern, number of redundant events per pattern
                if (!res.ErrorOccurred)
                {
                    csv.WriteLine(
                        (new List<string>()
                        {
                            res.InitialGraph.idHash()+"",
                            res.InitialGraph.Activities.Count+"",
                            res.InitialGraph.RelationsCount+"",
                            (res.InitialGraph.RelationsCount - res.CompleteApproachResult.GetRelationCount) + "",
                            res.CompleteRelationsRemovedCount+"",
                            res.EventsByPatternApproach.Count+"",
                            (res.InitialGraph.RelationsCount - res.PatternApproachResult.RelationsCount)+""
                        }).Aggregate((x, y) => x + "," + y)
                    );

                    combinedPatternStatistics.UpdateWith(res.PatternStatistics);
                    relationsRemovedByPatternsNotByCompleteSum.UnionWith(res.RelationsRemovedByPatternNotByCompleteApproach);
                    roundsSpentPatternApproachSum += res.PatternApproachRoundsSpent;
                    timeSpentCompleteApproach += res.CompleteApproachTimeSpent;
                    redundanciesLeftAfterPatternApproachCombined.UnionWith(res.PatternResultFullyRedundancyRemovedRelationsRemoved);
                    numberOfTraces += res.NumberOfTraces;
                    traceLengthSum += res.AverageTraceLength;
                    successCount++;
                }
                else
                { //write error
                    using (var errorOut = new StreamWriter(home + "\\" + errorPrefix + "Error" + errorCount + ".xml"))
                    {
                        var xml = DcrGraphExporter.ExportToXml(res.ErrorGraphContext);
                        errorOut.WriteLine($"<!-- {res.ErrorEvent} ({counter}) -->");
                        foreach (var before in res.EventsByPatternApproach.GetRange(0, res.EventsByPatternApproach.IndexOf(res.ErrorEvent)))
                        {
                            errorOut.WriteLine($"<!-- {before} -->");
                        }
                        if (res.ErrorTrace != null) 
                            errorOut.WriteLine($"<!-- {String.Join(" -> ", res.ErrorTrace)} -->");
                        errorOut.WriteLine(xml);
                    }
                    using (var errorOut =
                        new StreamWriter(home + "\\" + errorPrefix + "Error" + errorCount + "_original.xml"))
                    {
                        var xml = DcrGraphExporter.ExportToXml(res.InitialGraph);
                        errorOut.WriteLine(xml);
                    }
                    errorCount++;
                }
                counter++;
            }

            return new RedundancyRemoverComparer.ComparisonResult
            {
                PatternStatistics = combinedPatternStatistics,
                CompleteRelationsRemovedCount = completeApproach,
                PatternRelationsRemovedCount = patternApproach,
                RelationsRemovedByPatternNotByCompleteApproach = relationsRemovedByPatternsNotByCompleteSum,
                PatternApproachRoundsSpent = roundsSpentPatternApproachSum,
                CompleteApproachTimeSpent = timeSpentCompleteApproach,
                PatternResultFullyRedundancyRemovedRelationsRemoved = redundanciesLeftAfterPatternApproachCombined,
                NumberOfTraces = numberOfTraces / successCount,
                AverageTraceLength = traceLengthSum / successCount

            };
        }
    }
}
