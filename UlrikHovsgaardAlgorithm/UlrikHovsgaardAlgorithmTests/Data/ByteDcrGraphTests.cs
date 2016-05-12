using Microsoft.VisualStudio.TestTools.UnitTesting;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithmTests.Data
{
    [TestClass()]
    public class ByteDcrGraphTests
    {
        [TestMethod()]
        public void IsByteExcludedOrExecutedTest()
        {
            var res = ByteDcrGraph.IsByteExcludedOrExecuted(7); //111

            Assert.IsTrue(res);
        }

        [TestMethod()]
        public void IsByteExcludedOrExecutedTest2()
        {
            var res = ByteDcrGraph.IsByteExcludedOrExecuted(4); //111

            Assert.IsTrue(res);
        }

        [TestMethod()]
        public void IsByteExcludedOrExecutedTest3()
        {
            var res = ByteDcrGraph.IsByteExcludedOrExecuted(2); //111

            Assert.IsFalse(res);
        }

        [TestMethod()]
        public void IsByteExcludedOrNotPendingTest()
        {
            var res = ByteDcrGraph.IsByteExcludedOrNotPending(2); //111

            Assert.IsTrue(res);
        }

        [TestMethod()]
        public void IsByteExcludedOrNotPendingTest2()
        {
            var res = ByteDcrGraph.IsByteExcludedOrNotPending(4); //111

            Assert.IsTrue(res);
        }

        [TestMethod()]
        public void IsByteExcludedOrNotPendingTest3()
        {
            var res = ByteDcrGraph.IsByteExcludedOrNotPending(3); //111

            Assert.IsFalse(res);
        }
    }
}