using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithmTests.QualityMeasures
{
    [TestClass()]
    public class QualityDimensionRetrieverTests
    {
        [TestMethod()]
        public void RetrieveFitnessTest()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true};
            var activityB = new Activity("B", "somename2") { Included = false };
            var activityC = new Activity("C", "somename3") { Included = false };

            dcrGraph.AddActivities(activityA,activityB,activityC);

            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityB.Id, activityB.Id);

            var log = new Log();
            log.AddTrace(new LogTrace('A', 'B'));
            log.AddTrace(new LogTrace('A','B','C'));
            log.AddTrace(new LogTrace('A', 'B', 'C')); //duplicate trace should still count
            log.AddTrace(new LogTrace('A', 'B', 'C', 'A')); //illegal trace should count down

            //expecting fitness = 75%
            var qd = UlrikHovsgaardAlgorithm.QualityMeasures.QualityDimensionRetriever.Retrieve(dcrGraph, log);
            Assert.AreEqual(75d,qd.Fitness);
        }

        [TestMethod()]
        public void RetrievePrecisionTest()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = false };
            var activityC = new Activity("C", "somename3") { Included = false };

            dcrGraph.AddActivities(activityA, activityB, activityC);

            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityB.Id, activityB.Id);

            var log = new Log();
            log.AddTrace(new LogTrace('A', 'B'));
            log.AddTrace(new LogTrace('A', 'B', 'C'));
            log.AddTrace(new LogTrace('A', 'B', 'C')); //duplicate trace should not matter
            log.AddTrace(new LogTrace('A', 'B', 'C', 'A')); //illegal execution should count down

            //legal activities executed pr. state = 3
            // divided by illegal executed activities (1) + legal activities that could be executed (3 + 1) (c could be executed again.)

            //expecting precision 3/5 = 60%
            var qd = UlrikHovsgaardAlgorithm.QualityMeasures.QualityDimensionRetriever.Retrieve(dcrGraph, log);
            Assert.AreEqual(60d, qd.Precision);
        }

        [TestMethod()]
        public void RetrieveSimplicityTest()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = false };

            dcrGraph.AddActivities(activityA, activityB);

            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);

            var log = new Log(); //log does not matter for simplicity
            log.AddTrace(new LogTrace('A', 'B'));
            log.AddTrace(new LogTrace('A', 'B', 'C'));
            log.AddTrace(new LogTrace('A', 'B', 'C')); 
            log.AddTrace(new LogTrace('A', 'B', 'C', 'A')); 

            //S1 = amount of relations (1) / amount of possible relations (4n^2 - 3n = 10) = 0,10
            //S2 = amount of coupled relations (1) / possible coupled relations (n^2 = 4) = 0,25
            //S3 = amount of pending activities 1 / all activities (2) = 0,5 

            //expecting simplicity: 1 - (0,1*0,45 + 0,25*0,45 + 0,5 * 0,10)
            var qd = UlrikHovsgaardAlgorithm.QualityMeasures.QualityDimensionRetriever.Retrieve(dcrGraph, log);
            Assert.AreEqual(79.25d, qd.Simplicity);
        }

        [TestMethod()]
        public void RetrieveFitnessOnUnhealthyTest()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = false };

            dcrGraph.AddActivities(activityA, activityB, activityC);

            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityB.Id, activityB.Id);

            var log = new Log();
            log.AddTrace(new LogTrace('A', 'B'));
            log.AddTrace(new LogTrace('A', 'B', 'C'));
            log.AddTrace(new LogTrace('B')); //Should count down since A is not executed
            log.AddTrace(new LogTrace('A', 'B', 'C', 'A')); //illegal trace should count down

            //expecting fitness = 50%
            var qd = UlrikHovsgaardAlgorithm.QualityMeasures.QualityDimensionRetriever.Retrieve(dcrGraph, log);
            Assert.AreEqual(50d, qd.Fitness);
        }

        [TestMethod()]
        public void FitnessOnNestedGraph()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = true };
            var activityF = new Activity("F", "somename6") { Included = true };

            dcrGraph.AddActivities(activityA,activityB,activityC,activityD,activityE,activityF);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityD.Id);
            dcrGraph.AddCondition(activityE.Id, activityF.Id); //outgoing relation
            //ingoing relations
            dcrGraph.AddCondition(activityA.Id, activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityC, activityD, activityE });


            var log = new Log();
            log.AddTrace(new LogTrace('A', 'C','D'));
            log.AddTrace(new LogTrace('A', 'B', 'C'));
            log.AddTrace(new LogTrace('A', 'E', 'F')); 
            log.AddTrace(new LogTrace('C', 'E','D')); //illegal trace should count down

            //expecting fitness = 75%
            var qd = UlrikHovsgaardAlgorithm.QualityMeasures.QualityDimensionRetriever.Retrieve(dcrGraph, log);

            Assert.AreEqual(75d, qd.Fitness);
        }

        [TestMethod()]
        public void PrecisionOnNestedGraph()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = false };
            var activityF = new Activity("F", "somename6") { Included = false };

            dcrGraph.AddActivities(activityA, activityB, activityC, activityD, activityE, activityF);
            //ingoing relations
            dcrGraph.AddCondition(activityA.Id, activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);
            dcrGraph.AddCondition(activityC.Id, activityE.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityD.Id);
            dcrGraph.AddIncludeExclude(true, activityD.Id, activityE.Id);
            dcrGraph.AddIncludeExclude(true, activityE.Id, activityF.Id); //outgoing relation

            dcrGraph.AddIncludeExclude(false, activityA.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityC.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(false, activityD.Id, activityD.Id);
            dcrGraph.AddIncludeExclude(false, activityE.Id, activityE.Id); 
            //F can be run more than once

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityC, activityD, activityE });


            var log = new Log();
            log.AddTrace(new LogTrace('A', 'C', 'D', 'E', 'F')); // we don't execute 'F' twice

            log.AddTrace(new LogTrace('A', 'C', 'D', 'E', 'E')); //we illegally execute E

            //legal activities executed pr. state = 5
            // divided by illegal executed activities (1) + legal activities that could be executed (5 + 1) (F could be executed again.)

            //expecting precision 5/7
            var qd = UlrikHovsgaardAlgorithm.QualityMeasures.QualityDimensionRetriever.Retrieve(dcrGraph, log);
            Assert.AreEqual((5d/7d)*100, qd.Precision);
        }

        [TestMethod()]
        public void SimplicityOnNestedGraph()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true};
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = false };
            var activityF = new Activity("F", "somename6") { Included = false };

            dcrGraph.AddActivities(activityA, activityC, activityD, activityE, activityF);
            //ingoing relations
            dcrGraph.AddCondition(activityA.Id, activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);
            dcrGraph.AddCondition(activityC.Id, activityE.Id); //inside nested relation 
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityD.Id);
            dcrGraph.AddIncludeExclude(true, activityD.Id, activityE.Id);
            dcrGraph.AddIncludeExclude(true, activityE.Id, activityF.Id); //outgoing relation

            dcrGraph.AddIncludeExclude(false, activityA.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityC.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(false, activityD.Id, activityD.Id);
            dcrGraph.AddIncludeExclude(false, activityE.Id, activityE.Id);
            //F can be run more than once

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityC, activityD, activityE });


            var log = new Log();
            log.AddTrace(new LogTrace('A', 'C', 'D', 'E', 'F'));
            log.AddTrace(new LogTrace('A', 'C', 'D', 'E', 'E'));



            //S1 = amount of relations (9) / amount of possible relations n=6 (4n^2 - 3n = 126) = 9/126
            //S2 = amount of coupled relations (9) / possible coupled relations (n^2 = 36) = 0,25
            //S3 = amount of pending activities 1 / all activities (5) = 0,2    (pending activities should not count the nested graph)

            const double totalRelationsPart = 4.5 * (1.0 - (9.0 / 126.0)) / 10.0; //45% weight
            const double relationCouplesPart = 4.5 * (1.0 - 9.0/36.0) / 10.0; //45 % weight
            const double pendingPart = (1.0 - 1.0 / 5.0) / 10; // 10% weight


            //expecting simplicity: 1 - ((9/126)*0,45 + 0,25*0,45 + 0,2 * 0,10)
            const double expected = 100 *(totalRelationsPart + relationCouplesPart +pendingPart);
            var qd = UlrikHovsgaardAlgorithm.QualityMeasures.QualityDimensionRetriever.Retrieve(dcrGraph, log);
            Assert.AreEqual(expected, qd.Simplicity);
        }
    }
}