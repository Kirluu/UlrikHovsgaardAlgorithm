using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class Process
    {
        public string Id { get; set; }
        public HashSet<LogEvent> Alphabet { get; set; }
        public List<LogTrace> Traces { get; set; }
    }
}
