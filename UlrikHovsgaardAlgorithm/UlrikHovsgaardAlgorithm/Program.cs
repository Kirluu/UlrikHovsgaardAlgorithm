using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithm
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var tester = new TestClassForCSharpStuff();
            //tester.TestNestedGraphMaker();
            //tester.TestCopyMethod();
            //tester.TestUniqueTracesMethod();
            //tester.TestDictionaryAccessAndAddition();
            //tester.TestAreTracesEqualSingle();
            //tester.TestAreUniqueTracesEqual();
            //tester.TestCompareTracesWithSupplied();
            //tester.TestRedundancyRemover();
            //tester.TestRedundancyRemoverLimited();
            //tester.TestRedundancyRemoverExcludes();
            //tester.TestUniqueTracesMethodExcludes();
            //tester.ExhaustiveTest();
            //tester.TestUnhealthyInput();
            //tester.TestExportDcrGraphToXml();
            tester.TestOriginalLog();
            //tester.TestFinalStateMisplacement();
            //tester.TestActivityCreationLimitations();
            //tester.TestCanActivityEverBeIncluded();
            //tester.TestFlowerModel();
            //tester.TestQualityDimensionsRetriever();
            //tester.TestAlmostFlowerModel();
            //tester.TestThreadedTraceFindingWithOriginalTestLog();
            //tester.FlowerTestSyncVsThreaded();
            //tester.TestRetrieveIncludeRelationTrust();
            //tester.TestLogParserBpiChallenge2015();
            //tester.TestLogParserHospital();
            //tester.TestDcrGraphXmlParserFromDcrGraphsNet();
            //ParseDreyerLog();
            tester.ParseMortgageApplication();

            // TODO: Read from log
            // TODO: Build Processes, LogTraces and LogEvents

            // TODO: Run main algorithm
        }
    }
}
