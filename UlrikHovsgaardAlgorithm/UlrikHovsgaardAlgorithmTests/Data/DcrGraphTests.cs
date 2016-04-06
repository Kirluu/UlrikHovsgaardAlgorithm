using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm.Data.Tests
{
    [TestClass()]
    public class DcrGraphTests
    {
        [TestMethod()]
        public void GetActivityTest()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = false };

            dcrGraph.AddActivities(activityA, activityB, activityC);

            var retrievedActivity = dcrGraph.GetActivity(activityB.Id);

            Assert.AreSame(activityB,retrievedActivity);
        }

        [TestMethod()]
        public void GetNestedActivityTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = true };
            var activityF = new Activity("F", "somename6") { Included = true };

            dcrGraph.AddActivities(activityA, activityB, activityC, activityD, activityE, activityF);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityD.Id);
            dcrGraph.AddCondition(activityE.Id, activityF.Id); //outgoing relation
            //ingoing relations
            dcrGraph.AddCondition(activityA.Id, activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityC, activityD, activityE });

            var retrievedActivity = dcrGraph.GetActivity(activityC.Id);

            Assert.AreEqual(activityC.ToString(), retrievedActivity.ToString());
        }

        [TestMethod()]
        public void MakeNestedGraphTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = true };
            var activityF = new Activity("F", "somename6") { Included = true };

            dcrGraph.AddActivity(activityC.Id, activityC.Name);
            dcrGraph.AddActivity(activityD.Id, activityD.Name);
            dcrGraph.AddActivity(activityE.Id, activityE.Name);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityD.Id);
            dcrGraph.AddActivity(activityA.Id, activityA.Name);
            dcrGraph.AddActivity(activityB.Id, activityB.Name);
            dcrGraph.AddActivity(activityF.Id, activityF.Name);
            dcrGraph.AddCondition(activityE.Id, activityF.Id); //outgoing relation
            //ingoing relation
            dcrGraph.AddCondition(activityA.Id, activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityC, activityD, activityE });
            
            //we check that the Nested graph exists
            Assert.IsTrue(dcrGraph.Activities.Any(a => a.IsNestedGraph));
            //TODO: check the ingoing and outgoing relations are made correctly. And that we can find an activity within.
        }
        

        [TestMethod()]
        public void CopyTest()
        {
            DcrGraph dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = false };

            dcrGraph.AddActivities(activityA, activityB, activityC);

            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityC.Id);

            var copy = dcrGraph.Copy();

            Assert.AreEqual(dcrGraph.ToString(), copy.ToString());
        }

        [TestMethod()]
        public void CopyNestedTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };
            var activityD = new Activity("D", "somename4") { Included = false };
            var activityE = new Activity("E", "somename5") { Included = true };
            var activityF = new Activity("F", "somename6") { Included = true };

            dcrGraph.AddActivity(activityC.Id, activityC.Name);
            dcrGraph.AddActivity(activityD.Id, activityD.Name);
            dcrGraph.AddActivity(activityE.Id, activityE.Name);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityD.Id);
            dcrGraph.AddActivity(activityA.Id, activityA.Name);
            dcrGraph.AddActivity(activityB.Id, activityB.Name);
            dcrGraph.AddActivity(activityF.Id, activityF.Name);
            dcrGraph.AddCondition(activityE.Id, activityF.Id); //outgoing relation
            //ingoing relations
            dcrGraph.AddCondition(activityA.Id, activityC.Id);
            dcrGraph.AddCondition(activityA.Id, activityD.Id);
            dcrGraph.AddCondition(activityA.Id, activityE.Id);

            dcrGraph.MakeNestedGraph(new HashSet<Activity>() { activityC, activityD, activityE });

            var copy = dcrGraph.Copy();

            Assert.AreEqual(dcrGraph.ToString(), copy.ToString());
        }
        
        
    }
}