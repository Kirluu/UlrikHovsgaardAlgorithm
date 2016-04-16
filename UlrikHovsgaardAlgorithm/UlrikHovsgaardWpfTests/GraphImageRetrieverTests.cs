using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardWpf;

namespace UlrikHovsgaardWpfTests
{
    [TestClass()]
    public class GraphImageRetrieverTests
    {
        [TestMethod()]
        public void RetrieveTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true, Pending = true };
            var activityB = new Activity("B", "somename2") { Included = false };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.AddActivities(activityA, activityB, activityC);

            dcrGraph.AddResponse(activityB.Id, activityC.Id);

            var img = GraphImageRetriever.Retrieve(dcrGraph).Result;
            

            Assert.IsNotNull(img);
        }

        [TestMethod()]
        public void RetrieveImageWithIncludeRelationTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };

            dcrGraph.AddActivities(activityA, activityB);

            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);

            var img = GraphImageRetriever.Retrieve(dcrGraph).Result;




            Assert.IsNotNull(img);
        }

        [TestMethod()]
        public void RetrieveImageWithMilestoneRelationTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };

            dcrGraph.AddActivities(activityA, activityB);

            dcrGraph.AddMileStone(activityA.Id, activityB.Id);

            var img = GraphImageRetriever.Retrieve(dcrGraph).Result;
            

            Assert.IsNotNull(img);
        }

        [TestMethod()]
        public void RetrieveImageWithExcludeRelationTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };

            dcrGraph.AddActivities(activityA, activityB);

            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);

            var img = GraphImageRetriever.Retrieve(dcrGraph).Result;
            
            Assert.IsNotNull(img);
        }

        [TestMethod()]
        public void RetrieveImageWithResponseRelationTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };

            dcrGraph.AddActivities(activityA, activityB);

            dcrGraph.AddResponse(activityA.Id, activityB.Id);

            var img = GraphImageRetriever.Retrieve(dcrGraph).Result;

            Assert.IsNotNull(img);
        }

        [TestMethod()]
        public void RetrieveImageWithConditionRelationTest()
        {

            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };

            dcrGraph.AddActivities(activityA, activityB);

            dcrGraph.AddCondition(activityA.Id, activityB.Id);

            var img = GraphImageRetriever.Retrieve(dcrGraph).Result;

            Assert.IsNotNull(img);
        }
    }
}