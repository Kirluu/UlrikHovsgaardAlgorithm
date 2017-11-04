using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.Mining;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    /// <summary>
    /// The goal of this class is to run the comparisons with no overhead aside from running the complete RR
    /// as well as the pattern RR on the same start graph and comparing the amount of relations found by either method,
    /// finally summarizing the performances on all gives logs or graphs.
    /// </summary>
    public class RedundancyRemoverComparerTester
    {
        // TODO: Own RedundancyStatistics specific to comparison statistics?

        public (List<RedundancyRemoverComparer.ComparisonResult>, List<RedundancyRemoverComparer.ComparisonResult>) DoStatisticsComparisonRun(
            List<Log> logs, double goodResultThreshold)
        {
            return DoStatisticsComparisonRun(logs.AsParallel().Select(l =>
            {
                var contrAppr = new ContradictionApproach(new HashSet<Activity>(l.Alphabet.Select(e => new Activity(e.EventId, e.Name))));
                contrAppr.AddLog(l);
                return contrAppr.Graph;
            }).ToList(), goodResultThreshold);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>A list of error-scenarios and a list of sub-optimal results</returns>
        public (List<RedundancyRemoverComparer.ComparisonResult>, List<RedundancyRemoverComparer.ComparisonResult>) DoStatisticsComparisonRun(
            List<DcrGraph> graphs, double goodResultThreshold)
        {
            var errorsDiscovered = new List<RedundancyRemoverComparer.ComparisonResult>();
            var poorResults = new List<RedundancyRemoverComparer.ComparisonResult>();

            var patternTotal = 0;
            var completeTotal = 0;
            foreach (var dcr in graphs)
            {
                var dcrSimple = DcrGraphExporter.ExportToSimpleDcrGraph(dcr);

                // We want the bare minimum: Statistics and relation-counts --> Run stripped down version of comparison
                var res = RedundancyRemoverComparer.PerformComparisonGetStatistics(dcr, dcrSimple, performErrorDetection: false);
                var pat = res.PatternEventCount;
                var com = res.CompleteEventCount;
                patternTotal += pat;
                completeTotal += com;

                Console.WriteLine($"{pat / (double)com:P2} ({pat}/{com})");

                // TODO: Store the graph(s) --> Export somewhere? --> Folder with all error graphs + Folder with graphs with < X % catch-rate
                
                // Register errors 
                if (res.ErrorEvent != null)
                {
                    Console.WriteLine("ERROR!:\n" +
                                      $"{res.ErrorEvent}");
                    errorsDiscovered.Add(res);
                }
                // No error, but store as result if result didn't pass the threshold for a good result --> Source for new patterns
                else if (res.PatternEventCount / (double) res.CompleteEventCount < goodResultThreshold)
                {
                    poorResults.Add(res);
                }
            }

            Console.WriteLine("--------------------------------------------\n" +
                "TOTAL:\n" +
                $"Pattern approach: {patternTotal}, Complete approach: {completeTotal}");

            return (errorsDiscovered, poorResults);
        }

    }
}
