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

            // Apply complete redundancy-remover first:
            var completeRemover = new RedundancyRemover();
            var redundancyRemovedComplete = completeRemover.RemoveRedundancy(dcr, bgWorker);


        }

        private void ApplyPatterns(DcrGraphSimple dcr)
        {
            foreach (var act in dcr.Activities)
            {
                // If pending and never executable --> Remove all incoming Responses
                if (act.Pending && !dcr.IsEverExecutable(act))
                {
                    // Remove all incoming Responses
                    dcr.RemoveAllIncomingResponses(act);
                }
            }
        }

        private void ApplyBasicRedundancyRemovalLogic(DcrGraphSimple dcr)
        {
            // Remove everything that is excluded and never included
            foreach (var act in dcr.Activities)
            {
                // If excluded and never included
                if (!act.Included && dcr.IncludesInverted.ContainsKey(act))
                {
                    // Remove activity and all of its relations
                    dcr.MakeActivityDisappear(act);
                }
                // If included and never excluded --> remove all incoming includes
                else if (act.Included && !dcr.ExcludesInverted.ContainsKey(act))
                {
                    // Remove all incoming includes
                    dcr.RemoveAllIncomingIncludes(act);
                }
            }
        }
    }
}
