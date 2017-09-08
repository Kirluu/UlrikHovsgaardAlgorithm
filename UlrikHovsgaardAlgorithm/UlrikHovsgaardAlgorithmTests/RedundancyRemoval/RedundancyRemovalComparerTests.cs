using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithmTests.RedundancyRemoval
{
    [TestClass()]
    public class RedundancyRemovalComparerTests
    {
        [TestMethod()]
        public void PrimaryComparison()
        {
            //TestMortgageApplicationGraph();
            Test9ActivitiesAllIncludingEachOther();

            var dummy = 0;
            dummy++;
        }

        private void TestMortgageApplicationGraph()
        {
            var xml = Properties.Resources.mortgageGRAPH;
            var dcrGraph = XmlParser.ParseDcrGraph(xml);

            var comparer = new RedundancyRemoverComparer();

            comparer.PerformComparison(dcrGraph);
        }

        private void Test9ActivitiesAllIncludingEachOther()
        {
            var xml = Properties.Resources.AllInclusion9ActivitiesGraph;
            var dcrGraph = XmlParser.ParseDcrGraph(xml);

            var comparer = new RedundancyRemoverComparer();

            comparer.PerformComparison(dcrGraph);
        }
    }
}
