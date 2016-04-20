using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class LogStandard
    {
        public string Namespace { get; set; }
        public string TraceIdentifier { get; set; }
        public string TraceIdIdentifier { get; set; }
        public string EventIdentifier { get; set; }
        public string EventIdIdentifier { get; set; }
        public string EventNameIdentifier { get; set; }

        public LogStandard(string @namespace, string traceIdentifier, string traceIdIdentifier, string eventIdentifier,
            string eventIdIdentifier, string eventNameIdentifier)
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
