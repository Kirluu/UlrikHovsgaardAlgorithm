using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm.Data
{
    public enum DataType { Int, String }

    public class LogStandardEntry
    {
        public DataType DataType { get; }
        public string Name { get; }

        public LogStandardEntry(DataType type, string name)
        {
            DataType = type;
            Name = name;
        }
    }

    public class LogStandard
    {
        public string Namespace { get; }
        public string TraceIdentifier { get; }
        public LogStandardEntry TraceIdIdentifier { get; }
        public string EventIdentifier { get; }
        public LogStandardEntry EventIdIdentifier { get; }
        public LogStandardEntry EventNameIdentifier { get; }
        public LogStandardEntry ActorNameIdentifier { get; }

        public LogStandard(string @namespace, string traceIdentifier, LogStandardEntry traceIdIdentifier, string eventIdentifier,
            LogStandardEntry eventIdIdentifier, LogStandardEntry eventNameIdentifier, LogStandardEntry actorNameIdentifier)
        {
            Namespace = @namespace;
            TraceIdentifier = traceIdentifier;
            TraceIdIdentifier = traceIdIdentifier;
            EventIdentifier = eventIdentifier;
            EventIdIdentifier = eventIdIdentifier;
            EventNameIdentifier = eventNameIdentifier;
            ActorNameIdentifier = actorNameIdentifier;
        }
    }
}
