using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class Log
    {
        public string Id { get; set; }
        public HashSet<LogEvent> Alphabet { get; set; } = new HashSet<LogEvent>();
        public List<LogTrace> Traces { get; set; } = new List<LogTrace>();
    }
}
