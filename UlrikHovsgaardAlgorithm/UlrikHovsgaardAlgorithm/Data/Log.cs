using System;
using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class Log
    {
        public string Id { get; set; }
        public HashSet<LogEvent> Alphabet { get; set; } = new HashSet<LogEvent>();
        public List<LogTrace> Traces { get; set; } = new List<LogTrace>();

        public void AddTrace(LogTrace trace)
        {
            foreach (var logEvent in trace.Events)
            {
                Alphabet.Add(logEvent);
            }
            Traces.Add(trace);
        }

        public void AddEventToTrace(string traceId, LogEvent eve)
        {
            var searchResult = Traces.Find(x => x.Id == traceId);
            if (searchResult != null)
            {
                searchResult.Events.Add(eve);
            }
            else
            {
                throw new ArgumentException("No such trace");
            }
        }
    }
}
