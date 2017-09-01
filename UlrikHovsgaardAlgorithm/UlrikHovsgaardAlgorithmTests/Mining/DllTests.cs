using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiningAndRedundancyRemovalDll;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithmTests.Mining
{
    [TestClass]
    public class DllTests
    {
        [TestMethod]
        public void MineGraphTest()
        {
            var log = new Log();
            log.Alphabet = new HashSet<LogEvent>
            {
                new LogEvent("A", "Some action"),
                new LogEvent("B", "An other action"),
                new LogEvent("A", "Yet another action")
            };
            log.AddTrace(new LogTrace('A', 'B', 'C'));
            log.AddTrace(new LogTrace('A', 'B', 'B', 'C', 'B'));

            var logXml = Log.ExportToXml(log);
            var constraintViolationThreshold = 2.0;

            var graphXml = DCRMiningLibrary.MineGraph(logXml, constraintViolationThreshold, 0);

            var redundancyRemovedGraphXml = DCRMiningLibrary.RemoveRedundancy(graphXml);
            
        }
    }
}
