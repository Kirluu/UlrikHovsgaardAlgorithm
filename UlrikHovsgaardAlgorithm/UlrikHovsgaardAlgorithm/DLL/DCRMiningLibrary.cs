using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.DLL
{
    public class DCRMiningLibrary
    {

        public string MineGraph(Log log, double constraintViolationThreshold, int nestedGraphMinimumSize)
        {


            return new DCRResult(null, null).ExportToXml();
        }

        public string RemoveRedundancy(string dcrGraphXml)
        {


            return new DCRResult(null, null).ExportToXml();
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
                return null;
            }
        }
    }
}
