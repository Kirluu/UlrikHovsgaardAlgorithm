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
    public class ComparableListTests
    {
        [TestMethod()]
        public void ComparableListTest()
        {
            var set = new HashSet<ComparableList<int>>();

            set.Add(new ComparableList<int> { 1, 2, 3 });
            set.Add(new ComparableList<int> { 1, 2, 3 });


            Assert.IsTrue(set.Count == 1);
        }

        [TestMethod()]
        public void ComparableListTest2()
        {
            var set = new HashSet<ComparableList<int>>();

            set.Add(new ComparableList<int> { 1, 2, 3 });
            set.Add(new ComparableList<int> { 1, 3, 2 });


            Assert.IsTrue(set.Count == 2);
        }

        [TestMethod()]
        public void ComparableListTest3()
        {
            var set = new HashSet<ComparableList<int>>();

            set.Add(new ComparableList<int> { 1, 2, 3 });
            set.Add(new ComparableList<int> { 1, 3, 2 });


            Assert.IsTrue(set.Contains(new ComparableList<int> { 1, 3, 2 }));
        }
    }
}