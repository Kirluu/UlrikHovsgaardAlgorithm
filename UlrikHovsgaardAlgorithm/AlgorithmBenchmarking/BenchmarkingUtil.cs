using System;
using System.Collections.Generic;
using System.Linq;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardAlgorithm.Utils;

namespace AlgorithmBenchmarking
{
    class BenchmarkingUtil
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello world");
            const int relationsMax = 100;
            var graphs = GraphGenerator.Generate(8, relationsMax, 100, g =>
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

            var results = graphs.Select(g => RedundancyRemoverComparer.PerformComparisonWithPostEvaluation(g.ToDcrGraph()
                , g.ToDcrGraph()));

            Console.WriteLine("Ran tests");

            Console.Read();

        }

        static void Benchmark(List<DcrGraph> graphs) 
        {
        }
        
    }
}
