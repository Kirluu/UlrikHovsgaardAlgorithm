using System;
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

            var exhaust = new ContradictionApproach(new HashSet<Activity>() {a,new Activity("B","nameB")});

            exhaust.AddEvent("A", "1000");

            Assert.IsTrue(exhaust.Graph.GetIncludedActivities().Contains(a));
        }

        //Test that the Graph has a condition
        [TestMethod()]
        public void ConditionTest()
        {
            var a = new Activity("A", "nameA");

            var b = new Activity("B", "nameB");


            var exhaust = new ContradictionApproach(new HashSet<Activity>() { a, b });

            exhaust.AddEvent("A", "1000");
            exhaust.AddEvent("B", "1000");
            exhaust.Stop();

            HashSet<Activity> con;

            exhaust.Graph.Conditions.TryGetValue(a, out con);
            
            Assert.IsTrue(con.Contains(b));
        }

        //Test that the Graph does not have a condition to B
        [TestMethod()]
        public void ConditionNegativeTest()
        {
            var a = new Activity("A", "nameA");

            var b = new Activity("B", "nameB");


            var exhaust = new ContradictionApproach(new HashSet<Activity>() { a, b });
            
            exhaust.AddEvent("B", "1000");
            exhaust.Stop();

            HashSet<Activity> con;

            exhaust.Graph.Conditions.TryGetValue(a, out con);

            Assert.IsFalse(con.Contains(b));
        }

        //log of size 100.000 traces with 8 random events in each
        [TestMethod()]
        public void ExhaustiveWithBigDataLog()
        {
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'Z'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }

            var rnd = new Random();
            var inputLog = new Log();
            var traceId = 1000;
            var currentTrace = new LogTrace() {Id = traceId.ToString()};
            while (inputLog.Traces.Count < 100000)
            {
                currentTrace.Add(new LogEvent(activities.ElementAt(rnd.Next(activities.Count)).Id, ""));

                if (currentTrace.Events.Count == 8)
                {
                    inputLog.AddTrace(currentTrace);
                    traceId++;
                    currentTrace = (new LogTrace() {Id = traceId.ToString()});

                }
                
            }
            
            var exAl = new ContradictionApproach(activities);

            foreach (var trace in inputLog.Traces)
            {
                exAl.AddTrace(trace);
            }

            Assert.IsTrue(true);
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
            dcrGraph.AddExclude(false, activityA.Id, activityB.Id);
            dcrGraph.AddExclude(false, activityA.Id, activityC.Id);
            dcrGraph.AddExclude(false, activityA.Id, activityD.Id);
            dcrGraph.AddExclude(false, activityA.Id, activityE.Id);
            dcrGraph.AddExclude(false, activityA.Id, activityB.Id);

            dcrGraph.AddExclude(false, activityD.Id, activityF.Id); //from in to out
            dcrGraph.AddMileStone(activityF.Id, activityG.Id);

            //From G to all inner and F
            dcrGraph.AddCondition(activityG.Id, activityB.Id);
            dcrGraph.AddCondition(activityG.Id, activityC.Id);
            dcrGraph.AddCondition(activityG.Id, activityD.Id);
            dcrGraph.AddCondition(activityG.Id, activityE.Id);
            dcrGraph.AddCondition(activityG.Id, activityF.Id);

            var exhaust = new ContradictionApproach(dcrGraph.Activities) { Graph = dcrGraph };

            exhaust.Graph = ContradictionApproach.CreateNests(exhaust.Graph);
            
            Assert.IsTrue(exhaust.Graph.Activities.Any(a => a.IsNestedGraph));
        }
        
    }
}