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

        public static string ExportToXml(Log log)
        {
            var logXml = string.Format("<log id=\"{0}\">", log.Id);

            foreach (var trace in log.Traces)
            {
                logXml += string.Format("<trace id=\"{0}\">", trace.Id);
                foreach (var logEvent in trace.Events)
                {
                    logXml += string.Format("<event id=\"{0}\" name=\"{1}\"/>", logEvent.IdOfActivity, logEvent.Name);
                }
                logXml += "</trace>";
            }
            logXml += "</log>";
            return logXml;
        }
    }
}
