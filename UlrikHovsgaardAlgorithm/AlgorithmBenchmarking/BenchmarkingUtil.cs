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
            var graphs = GraphGenerator.Generate(8, relationsMax, 1000, g =>
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
            var errors = new List<RedundancyRemoverComparer.ComparisonResult>();
            foreach (var res in results)
            {
                Console.WriteLine("----------------------");
                if (res.ErrorOccurred)
                {
                    Console.WriteLine("ERROR!");
                    errors.Add(res);
                }
                totalPatternRedundancies += res.PatternEventCount;
                totalCompleteRedundancies += res.CompleteEventCount;
                Console.WriteLine($"Pattern approach redundancy events: {res.PatternEventCount}");
                Console.WriteLine($"Complete approach redundancy events: {res.CompleteEventCount}");
                Console.WriteLine($"Pattern / Complete = {(res.CompleteEventCount == 0 ? 100.0 : (res.PatternEventCount + 0.0) / (res.CompleteEventCount + 0.0))}");
            }

            Console.WriteLine($"Final score: {totalPatternRedundancies / (totalCompleteRedundancies + 0.0)}");
            Console.WriteLine("-------------------ERRRORS------------------------");
            var errorCount = 0;
            foreach (var res in errors)
            {
                errorCount++;
                Console.WriteLine(res.ErrorEvent);
                var xml = DcrGraphExporter.ExportToXml(res.ErrorGraphContext);
                using (var fileOut = new StreamWriter($"C:\\uni\\graphs\\error{errorCount}", true))
                {
                    fileOut.WriteLine($"<!-- {res.ErrorEvent} -->");
                    fileOut.Write(xml);
                    fileOut.WriteLine();
                } 

            }
            Console.Read();

        }

        static void Benchmark(List<DcrGraph> graphs) 
        {
        }
        
    }
}
