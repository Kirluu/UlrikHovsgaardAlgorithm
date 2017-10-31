using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace RedundancyRemoverComparerWpf.DataClasses
{
    public struct TestableGraph
    {
        public string Name { get; private set; }
        public DcrGraph Graph { get; private set; }
        public DcrGraph RedundancyRemovedGraph { get; private set; }

        public TestableGraph(string name, DcrGraph dcrGraph, DcrGraph redundancyRemovedGraph = null)
        {
            Name = name;
            Graph = dcrGraph;
            RedundancyRemovedGraph = redundancyRemovedGraph;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
