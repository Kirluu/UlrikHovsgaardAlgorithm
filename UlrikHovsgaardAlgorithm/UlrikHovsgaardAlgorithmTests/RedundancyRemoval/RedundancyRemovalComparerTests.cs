using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Export;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithmTests.RedundancyRemoval
{
    [TestClass()]
    public class RedundancyRemovalComparerTests
    {
        [TestMethod()]
        public void TestMortgageApplicationGraph()
        {
            var xml = Properties.Resources.mortgageGRAPH;
            var dcrGraph = XmlParser.ParseDcrGraph(xml);

            //Console.WriteLine(dcrGraph.ToString());
            Console.WriteLine("Graph mined from a log built from the Mortgage Application graph on dcr.itu.dk\n\n");

            var comparer = new RedundancyRemoverComparer();

            comparer.PerformComparison(dcrGraph);
        }

        [TestMethod()]
        public void Test9ActivitiesAllIncludingEachOther()
        {
            var xml = Properties.Resources.AllInclusion9ActivitiesGraph;
            var dcrGraph = XmlParser.ParseDcrGraph(xml);

            var comparer = new RedundancyRemoverComparer();

            comparer.PerformComparison(dcrGraph);
        }

        [TestMethod()]
        public void CopySanityCheck()
        {
            var xml = Properties.Resources.mortgageGRAPH;
            var dcrGraph = XmlParser.ParseDcrGraph(xml);
            var simple = DcrGraphExporter.ExportToSimpleDcrGraph(dcrGraph);
            var copy = simple.Copy();

            var first = copy.Includes.First();
            copy.Includes.Remove(first.Key);

            Assert.IsTrue(!simple.Equals(copy) && !copy.Equals(simple));
        }
    }
}
