using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.QualityMeasures;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardAlgorithm.Utils;

namespace AlgorithmBenchmarking
{
    class BenchmarkingUtil
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world");
            const int relationsMax = 80;
            var graphs = GraphGenerator.Generate(8, relationsMax, 50, g =>
            {
                var remover = new RedundancyRemover();
                var other = g.ToDcrGraph();
                if (other.GetRelationCount == relationsMax)
                    Console.WriteLine("Stuff is off");
                Console.WriteLine("Removing redundancy...");
                var unique = new UniqueTraceFinder(new ByteDcrGraph(g));
                if (unique.IsNoAcceptingTrace())
                    return false;
                var (finalGraph, res) = remover.RemoveRedundancyInner(other);
                return remover.RedundantActivitiesFound > 0 || remover.RedundantRelationsFound > 0;
            });
            Console.WriteLine("Running tests");
            
            

            Console.WriteLine("Ran tests");
            
            var home = "C:\\Users\\christian";
            var minedGraphs = new List<DcrGraph>();
            using (var csv = new StreamWriter(home + "\\dcr_results_generated.csv"))
            {
                var results = graphs.Select(g => RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g.ToDcrGraph()));
                WriteResults(results, csv, home, "generated");
                foreach (var res in results)
                {
                    var logGenerator = new LogGenerator9001(70, res.InitialGraph.ToDcrGraph());
                    var traces = logGenerator.GenerateLog(250);
                    var miner = new ContradictionApproach(res.InitialGraph.Activities);
                    traces.ForEach(trace => miner.AddTrace(trace));
                    minedGraphs.Add(miner.Graph);
                }
            }
            Console.WriteLine("Beginning mining approach...");
            using (var csv = new StreamWriter(home + "\\dcr_results_mined.csv"))
            {
                WriteResults(
                    minedGraphs.Select(g => RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g)),
                    csv, home, "mined"
                );
            }
            Console.WriteLine("DONE!");
            Console.Read();
        }

        static void WriteResults(IEnumerable<RedundancyRemoverComparer.ComparisonResult> results, StreamWriter csv, string home, string errorPrefix)
        {
            int errorCount = 0;
            foreach (var res in results)
            {
                Console.WriteLine("----------------------");
                if (res.ErrorOccurred)
                {
                    Console.WriteLine("ERROR!");
                }
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
                            res.EventsByPatternApproach.Count+"",
                            (res.InitialGraph.RelationsCount - res.PatternApproachResult.RelationsCount)+""
                        }).Aggregate((x, y) => x + "," + y)
                    );
                }
                else
                { //write error
                    using (var errorOut = new StreamWriter(home + "\\" + errorPrefix + "Error" + errorCount++ + ".xml"))
                    {
                        var xml = DcrGraphExporter.ExportToXml(res.ErrorGraphContext);
                        errorOut.WriteLine($"<!-- {res.ErrorEvent} -->");
                        errorOut.WriteLine(xml);
                    }
                }
            }
        }
    }
}
