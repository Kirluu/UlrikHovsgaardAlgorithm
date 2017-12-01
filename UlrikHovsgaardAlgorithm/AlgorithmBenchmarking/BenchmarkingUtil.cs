using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardAlgorithm.Utils;

namespace AlgorithmBenchmarking
{
    class BenchmarkingUtil
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world");
            const int relationsMax = 150;
            var graphs = GraphGenerator.Generate(8, relationsMax, 10, g =>
            {
                var remover = new RedundancyRemover();
                var other = g.ToDcrGraph();
                if (other.GetRelationCount == relationsMax)
                    Console.WriteLine("Stuff is off");
                Console.WriteLine("Removing redundancy...");
                var (finalGraph, res) = remover.RemoveRedundancyInner(other);
                return remover.RedundantActivitiesFound > 0 || remover.RedundantRelationsFound > 0;
            });
            Console.WriteLine("Running tests");
            
            var results = graphs.Select(g => RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g.ToDcrGraph()));

            Console.WriteLine("Ran tests");

            int totalPatternRedundancies = 0;
            int totalCompleteRedundancies = 0;
            var errorCount = 0;
            var home = "C:\\Users\\christian";
            using (var csv = new StreamWriter(home + "\\dcr_results.csv"))
            {
                foreach (var res in results)
                {
                    Console.WriteLine("----------------------");
                    if (res.ErrorOccurred)
                    {
                        Console.WriteLine("ERROR!");
                    }
                    totalPatternRedundancies += res.PatternEventCount;
                    totalCompleteRedundancies += res.CompleteEventCount;
                    Console.WriteLine($"Pattern approach redundancy events: {res.PatternEventCount}");
                    Console.WriteLine($"Complete approach redundancy events: {res.CompleteEventCount}");
                    Console.WriteLine($"Pattern / Complete = {(res.CompleteEventCount == 0 ? 100.0 : (res.PatternEventCount + 0.0) / (res.CompleteEventCount + 0.0))}");
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
                            res.CompleteEventCount+"",
                            res.EventsByPatternApproach+"",
                            (res.InitialGraph.RelationsCount - res.PatternApproachResult.RelationsCount)+""
                            }).Aggregate((x, y) => x + "," + y)
                        );
                    } else
                    { //write error
                        using (var errorOut = new StreamWriter(home + "\\error" + errorCount++ + ".xml"))
                        {
                            var xml = DcrGraphExporter.ExportToXml(res.ErrorGraphContext);
                            errorOut.WriteLine($"<!-- {res.ErrorEvent} -->");
                            errorOut.WriteLine(xml);
                        }
                    }
                }

                Console.WriteLine($"Final score: {totalPatternRedundancies / (totalCompleteRedundancies + 0.0)}");
                Console.WriteLine("-------------------ERRRORS------------------------");
                Console.Read();
            }
        }

        static void Benchmark(List<DcrGraph> graphs) 
        {
        }
        
    }
}
