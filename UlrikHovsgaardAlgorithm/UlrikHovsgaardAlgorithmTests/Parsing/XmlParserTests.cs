using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace UlrikHovsgaardAlgorithm.Parsing
{
    [TestClass()]
    public class XmlParserTests
    {
        [TestMethod()]
        public void ParseLogTest()
        {
            
            var log = XmlParser.ParseLog(Properties.Resources.BPIC15_1_xes);
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