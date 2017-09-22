using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace RedundancyRemoverComparerWpf.DataClasses
{
    public class TestableGraph
    {
        public string Name { get; set; }
        public DcrGraph Graph { get; set; }

        public TestableGraph(string name, DcrGraph dcrGraph)
        {
            Name = name;
            Graph = dcrGraph;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
