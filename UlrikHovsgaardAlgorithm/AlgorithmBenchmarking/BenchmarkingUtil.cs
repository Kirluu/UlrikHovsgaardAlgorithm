using System;
using System.Collections.Generic;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace AlgorithmBenchmarking
{
    class BenchmarkingUtil
    {
        static void Main(string[] args)
        {
            
            Console.WriteLine("Hello World!");
        }

        static void Benchmark(List<DcrGraph> graphs) 
        {
            foreach (var graph in graphs)
            {
                var comparer = new RedundancyRemoverComparer();
                RedundancyRemoverComparer.
            }
        }
    }
}
