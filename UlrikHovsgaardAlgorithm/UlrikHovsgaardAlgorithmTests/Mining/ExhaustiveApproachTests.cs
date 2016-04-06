using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;

namespace UlrikHovsgaardAlgorithmTests.Mining
{
    [TestClass()]
    public class ExhaustiveApproachTests
    {
        //Test that the inner graph contains the included event A.
        [TestMethod()]
        public void AddEventTest()
        {
            var a = new Activity("A", "nameA");

            var exhaust = new ExhaustiveApproach(new HashSet<Activity>() {a,new Activity("B","nameB")});

            exhaust.AddEvent("A", "1000");

            Assert.IsTrue(exhaust.Graph.GetIncludedActivities().Contains(a));
        }
        
        [TestMethod()]
        public void CreateNestsTest()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = true };
            var activityE = new Activity("E", "somename5") { Included = true, Pending = true };
            var activityF = new Activity("F", "somename6") { Included = true };
            var activityG = new Activity("G", "somename7") { Included = true };

            dcrGraph.AddActivities(activityA, activityB, activityC, activityD, activityE, activityF, activityG);

            dcrGraph.AddResponse(activityB.Id, activityC.Id); //inner nest condition

            //From A to all inner
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityD.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityE.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);

            dcrGraph.AddIncludeExclude(false, activityD.Id, activityF.Id); //from in to out
            dcrGraph.AddMileStone(activityF.Id, activityG.Id);

            //From G to all inner and F
            dcrGraph.AddCondition(activityG.Id, activityB.Id);
            dcrGraph.AddCondition(activityG.Id, activityC.Id);
            dcrGraph.AddCondition(activityG.Id, activityD.Id);
            dcrGraph.AddCondition(activityG.Id, activityE.Id);
            dcrGraph.AddCondition(activityG.Id, activityF.Id);

            var exhaust = new ExhaustiveApproach(dcrGraph.Activities) { Graph = dcrGraph };

            exhaust.CreateNests();

            Assert.IsTrue(exhaust.Graph.Activities.Any(a => a.IsNestedGraph));
        }
        
    }
}