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
            var logXml = "<log>\n";

            foreach (var trace in log.Traces)
            {
                logXml += "\t<trace>\n";
                logXml += string.Format("\t\t<string key=\"id\" value=\"{0}\"/>\n", trace.Id);
                foreach (var logEvent in trace.Events)
                {
                    logXml += "\t\t<event>\n";
                    logXml += string.Format("\t\t\t<string key=\"id\" value=\"{0}\"/>\n", logEvent.IdOfActivity);
                    logXml += string.Format("\t\t\t<string key=\"name\" value=\"{0}\"/>\n", logEvent.Name);
                    logXml += "\t\t</event>\n";
                }
                logXml += "\t</trace>\n";
            }
            logXml += "</log>";
            return logXml;
        }
    }
}
