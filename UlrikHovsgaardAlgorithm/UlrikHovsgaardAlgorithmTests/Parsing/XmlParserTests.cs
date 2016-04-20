using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Parsing;

namespace UlrikHovsgaardAlgorithmTests.Parsing
{
    [TestClass()]
    public class XmlParserTests
    {
        [TestMethod()]
        public void ParseLogTest()
        {

            var log = XmlParser.ParseLog(new LogStandard("http://www.xes-standard.org/", "trace", "conceptName", "event", "conceptName", "activityNameEN"), UlrikHovsgaardAlgorithm.Properties.Resources.BPIC15_small);
            Console.WriteLine("Finished parsing " + log.Traces.Count);
            foreach (var trace in log.Traces.First().Events)
            {
                Console.WriteLine("Example trace: " + log.Traces.First().Id);
                Console.Write("ID: " + trace.IdOfActivity + ", Name: " + trace.Name + "   |   ");
            }
            Console.ReadLine();

            
            Assert.IsTrue(log.Traces.Any(t => t.Events.Count > 5));

        }
    }
}