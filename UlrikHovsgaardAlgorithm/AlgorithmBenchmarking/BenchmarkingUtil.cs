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

        public static void Main(string[] args)
        {
            const int relationsMax = 80;
            var graphs = GraphGenerator.Generate(8, relationsMax, 1000, g =>
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
            });
            Console.WriteLine("Running tests");
            
            

            Console.WriteLine("Ran tests");
            
            var home = "C:\\RedundancyOutputs";
            var minedGraphs = new List<DcrGraph>();
            int completeGenerated = 0;
            int patternGenerated = 0;
            int completeMined = 0;
            int patternMined = 0;
            using (var csv = new StreamWriter(home + "\\dcr_results_generated.csv"))
            {
                var results = new List<RedundancyRemoverComparer.ComparisonResult>();
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
                    results.Add(RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g.ToDcrGraph()));
                    counter++;
                }

                LanguageDifferenceCountPreMined = RedundancyRemoverComparer.GlobalPatternNotSameLanguageAsCompleteCounter;

                //var results = graphs.Select(g => RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g.ToDcrGraph()));
                var both = WriteResults(results, csv, home, "generated");
                completeGenerated = both.Item1;
                patternGenerated = both.Item2;
                foreach (var res in results)
                {
                    var logGenerator = new LogGenerator9001(70, res.InitialGraph.ToDcrGraph());
                    var traces = logGenerator.GenerateLog(150);
                    if (traces == null)
                        continue;
                    var miner = new ContradictionApproach(res.InitialGraph.Activities);
                    traces.ForEach(trace => miner.AddTrace(trace));
                    minedGraphs.Add(miner.Graph);
                }
            }
            Console.WriteLine("Beginning mining approach...");
            using (var csv = new StreamWriter(home + "\\dcr_results_mined.csv"))
            {
                Console.WriteLine($"Number of mined graphs: {minedGraphs.Count}");
                var both = WriteResults(
                    minedGraphs.Select(g => RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g)),
                    csv, home, "mined"
                );
                completeMined = both.Item1;
                patternMined = both.Item2;
            }

            LanguageDifferenceCountInclAll = RedundancyRemoverComparer.GlobalPatternNotSameLanguageAsCompleteCounter;

            var percentage = completeGenerated == 0 ? 1.0 : (double)patternGenerated / completeGenerated;
            Console.WriteLine($"Generated approach: Pattern / Complete = {percentage}");
            percentage = completeMined == 0 ? 1.0 : (double)patternMined / completeMined;
            Console.WriteLine($"Mined approach: Pattern / Complete = {percentage}");
            Console.WriteLine($"Language difference between Patter and Complete approach: Before Mined graphs: {LanguageDifferenceCountPreMined}, After: {LanguageDifferenceCountInclAll}");
            Console.WriteLine("DONE!");
            Console.Read();
        }

        public static (int, int) WriteResults(IEnumerable<RedundancyRemoverComparer.ComparisonResult> results, StreamWriter csv, string home, string errorPrefix)
        {
            int errorCount = 0;
            int completeApproach = 0;
            int patternApproach = 0;
            int counter = 0;
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
                if (res.ErrorEvent == null)
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
            return (completeApproach, patternApproach);
        }
    }
}
