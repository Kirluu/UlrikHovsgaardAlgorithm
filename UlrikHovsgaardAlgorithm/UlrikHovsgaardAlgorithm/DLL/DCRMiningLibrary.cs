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

namespace UlrikHovsgaardAlgorithm.DLL
{
    public class DCRMiningLibrary
    {

        public string MineGraph(string logString, double constraintViolationThreshold, int nestedGraphMinimumSize)
        {
            Threshold.Value = constraintViolationThreshold;

            var log = XmlParser.ParseLog(LogStandard.GetDefault(), logString);
            
            var approach = new ContradictionApproach(new HashSet<Activity>(log.Alphabet.Select(logEvent => new Activity(logEvent.IdOfActivity, logEvent.Name))));
            approach.AddLog(log);

            var graph = approach.Graph;
            var measures = QualityDimensionRetriever.Retrieve(graph, log);

            return new DCRResult(graph, measures).ExportToXml();
        }

        public string RemoveRedundancy(string dcrGraphXml)
        {
            var graph = XmlParser.ParseDcrGraph(dcrGraphXml);

            var redundancyRemovedGraph = new RedundancyRemover().RemoveRedundancy(graph);

            return new DCRResult(redundancyRemovedGraph, null).ExportToXml();
        }

        // Return-format
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
