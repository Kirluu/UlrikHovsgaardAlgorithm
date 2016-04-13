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

            var img = GraphImageRetriever.Retrieve(dcrGraph);



            Assert.IsNotNull(img);
        }
    }
}