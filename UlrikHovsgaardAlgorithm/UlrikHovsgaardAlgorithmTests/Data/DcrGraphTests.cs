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
            Assert.Fail();
        }

        [TestMethod()]
        public void AddActivityTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddActivityTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddRolesToActivityTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RemoveActivityFromOuterGraphTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RemoveActivityTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RemoveActivityFromNestTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetPendingTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetIncludedTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void SetExecutedTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddIncludeExcludeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddResponseTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddConditionTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RemoveConditionTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AddMileStoneTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RemoveIncludeExcludeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetRunnableActivitiesTest()
        {
            Assert.Fail();
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
        public void ExecuteTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void IsFinalStateTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetIncludedActivitiesTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ConvertToDictionaryActivityHashSetActivityTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CanActivityEverBeIncludedTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ActivityHasRelationsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void InRelationTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void InRelationTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void AreInEqualStateTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void HashDcrGraphTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void HashActivityTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void CopyTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetIncludeOrExcludeRelationTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ExportToXmlTest()
        {
            Assert.Fail();
        }
        
    }
}