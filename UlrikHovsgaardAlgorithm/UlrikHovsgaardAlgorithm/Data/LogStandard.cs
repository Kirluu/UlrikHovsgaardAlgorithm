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
        public DataType DataType { get; set; }
        public string Name { get; set; }

        public LogStandardEntry(DataType type, string name)
        {
            DataType = type;
            Name = name;
        }
    }

    public class LogStandard
    {
        public string Namespace { get; set; }
        public string TraceIdentifier { get; set; }
        public LogStandardEntry TraceIdIdentifier { get; set; }
        public string EventIdentifier { get; set; }
        public LogStandardEntry EventIdIdentifier { get; set; }
        public LogStandardEntry EventNameIdentifier { get; set; }
        public LogStandardEntry ActorNameIdentifier { get; set; }

        public LogStandard(string @namespace, string traceIdentifier, LogStandardEntry traceIdIdentifier, string eventIdentifier,
            LogStandardEntry eventIdIdentifier, LogStandardEntry eventNameIdentifier)
        {
            Namespace = @namespace;
            TraceIdentifier = traceIdentifier;
            TraceIdIdentifier = traceIdIdentifier;
            EventIdentifier = eventIdentifier;
            EventIdIdentifier = eventIdIdentifier;
            EventNameIdentifier = eventNameIdentifier;
        }
    }
}
