using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Export;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    public class RedundancyRemoverComparer
    {


        public void PerformComparison(DcrGraph dcr, BackgroundWorker bgWorker = null)
        {
            var dcrSimple = DcrGraphExporter.ExportToSimpleDcrGraph(dcr);

            if (!dcrSimple.SanityCheck())
            {
                Console.WriteLine($"OH BOY - RelationsCount: {dcrSimple.RelationsCount}");
            }

            // Apply complete redundancy-remover first:
            var completeRemover = new RedundancyRemover();
            var redundancyRemovedGraph = completeRemover.RemoveRedundancy(dcr, bgWorker);
            var redundantRelationsCount = completeRemover.RedundantRelationsFound;
            var redundantActivitiesCount = completeRemover.RedundantActivitiesFound;

            // Basic-optimize simple-graph:
            var (basicRelationsRemovedCount, basicActivitiesRemovedCount)
                = ApplyBasicRedundancyRemovalLogic(dcrSimple); // Modifies simple
            //var basicRelationsRemovedCount = tuple.Item1;
            //var basicActivitiesRemovedCount = tuple.Item2;

            if (!dcrSimple.SanityCheck())
            {
                Console.WriteLine($"OH BOY - RelationsCount: {dcrSimple.RelationsCount}");
            }

            // Pattern-application:
            var patternRelationsRemoved = ApplyPatterns(dcrSimple);

            if (!dcrSimple.SanityCheck())
            {
                Console.WriteLine($"OH BOY - RelationsCount: {dcrSimple.RelationsCount}");
            }

            var totalPatternApproachRelationsRemoved = basicRelationsRemovedCount + patternRelationsRemoved;

            // Comparison
            Console.WriteLine(
                $"Pattern approach detected {(totalPatternApproachRelationsRemoved / (double)redundantRelationsCount):P2} " +
                $"({totalPatternApproachRelationsRemoved} / {redundantRelationsCount})");

            Console.WriteLine($"Relations left using pattern-searcher: {dcrSimple.RelationsCount}");

            // Inform about specific relations "missed" by pattern-approach
        }

        /// <summary>
        /// Applies all our great patterns.
        /// </summary>
        /// <returns>Amount of relations removed</returns>
        private int ApplyPatterns(DcrGraphSimple dcr)
        {
            var relationsRemoved = 0;

            foreach (var act in dcr.Activities)
            {
                // If pending and never executable --> Remove all incoming Responses
                if (act.Pending && !dcr.IsEverExecutable(act))
                {
                    // Remove all incoming Responses
                    relationsRemoved += dcr.RemoveAllIncomingResponses(act);
                    Console.WriteLine($"SHOULDNT HAPPEN");
                }
            }

            return relationsRemoved;
        }

        private (int, int) ApplyBasicRedundancyRemovalLogic(DcrGraphSimple dcr)
        {
            var relationsRemoved = 0;
            var activitiesRemoved = 0;

            // Remove everything that is excluded and never included
            foreach (var act in dcr.Activities.ToArray())
            {
                // If excluded and never included
                if (!act.Included && !dcr.IncludesInverted.ContainsKey(act))
                {
                    // Remove activity and all of its relations
                    relationsRemoved += dcr.MakeActivityDisappear(act);
                    Console.WriteLine($"Excluded activity rule: Removed {relationsRemoved} relations in total");
                    activitiesRemoved++;
                }
                // If included and never excluded --> remove all incoming includes
                else if (act.Included && !dcr.ExcludesInverted.ContainsKey(act))
                {
                    // Remove all incoming includes
                    relationsRemoved += dcr.RemoveAllIncomingIncludes(act);
                    Console.WriteLine($"Always Included activity rule: Removed {relationsRemoved} relations in total");
                }
            }

            return (relationsRemoved, activitiesRemoved);
        }
    }
}
