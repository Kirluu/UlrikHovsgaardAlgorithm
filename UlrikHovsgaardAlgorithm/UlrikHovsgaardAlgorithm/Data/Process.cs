using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class Process
    {
        public string Id { get; set; }
        public HashSet<LogEvent> Alphabet { get; set; }
        public List<LogTrace> Traces { get; set; }
    }
}
