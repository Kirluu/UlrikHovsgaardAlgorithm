using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.QualityMeasures;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace MiningAndRedundancyRemovalDll
{
    public class DCRMiningLibrary
    {
        public static DCRResult MineGraph(Log log, double threshold) 
        {
            Threshold.Value = threshold;

            var approach = new ContradictionApproach(new HashSet<Activity>(log.Alphabet.Select(logEvent => new Activity(logEvent.IdOfActivity, logEvent.Name))));
            approach.AddLog(log);

            var graph = approach.Graph;
            var measures = QualityDimensionRetriever.Retrieve(graph, log);

            return new DCRResult(graph, measures);
        }

        public static DCRResult MineGraph(string log, double threshold) 
        {
            return MineGraph(XmlParser.ParseLog(LogStandard.GetDefault(), log), threshold);
        }

        public static DCRResult RemoveRedundancy(DCRResult r)
        {
            var redundancyRemovedGraph = new RedundancyRemover().RemoveRedundancy(r.DCR);

            var onlySimplicity = new QualityDimensions
            {
                Fitness = -1,
                Precision = -1,
                Generality = -1,
                Simplicity = QualityDimensionRetriever.GetSimplicityNew(redundancyRemovedGraph)
            };

            return new DCRResult(redundancyRemovedGraph, onlySimplicity);
        }

        /// <summary>
        /// Return format for the library
        /// </summary>
        public class DCRResult
        {
            public DCRResult(DcrGraph graph, QualityDimensions measures)
            {
                DCR = graph;
                Measures = measures;
            }

            public DcrGraph DCR { get; private set; }
            public QualityDimensions Measures { get; private set; }

            public string ExportToXml()
            {
                var xml = "<ulrikhovsgaardoutput>\n";

                xml += "<measures>\n";
                xml += $"<fitness>{ Measures.Fitness }</fitness>\n";
                xml += $"<precision>{ Measures.Precision }</precision>\n";
                xml += $"<simplicity>{ Measures.Simplicity }</simplicity>\n";
                xml += "</measures>\n";

                xml += DCR.ExportToXml();

                xml += "</ulrikhovsgaardoutput>";

                return xml;
            }
        }
    }
}
