using System.Collections.Generic;
using System.Linq;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class LogTrace
    {
        public string Id { get; set; }
        public List<LogEvent> Events { get; set; }

        public LogTrace()
        {
            Events = new List<LogEvent>();
        }

        public void Add(LogEvent e)
        {
            Events.Add(e);
        }

        public LogTrace Copy()
        {
            var copy = new LogTrace { Events = new List<LogEvent>() };
            foreach (var logEvent in Events)
            {
                copy.Add(new LogEvent
                {
                    Id = logEvent.Id,
                    NameOfActivity = logEvent.NameOfActivity,
                    ActorName = logEvent.ActorName,
                    RoleName = logEvent.RoleName,
                    TimeOfExecution = logEvent.TimeOfExecution
                });
            }
            return copy;
        }

        public override bool Equals(object obj)
        {
            LogTrace otherTrace = obj as LogTrace;
            if (otherTrace == null || Events.Count != otherTrace.Events.Count)
            {
                return false;
            }
            return !Events.Where((t, i) => !t.Equals(otherTrace.Events[i])).Any();
        }

        // String representation
        public string ToStringForm()
        {
            var returnString = "";
            var firstIte = true;

            foreach (var e in Events)
            {
                returnString += (firstIte ? "" : ";") + e.Id;
                firstIte = false;
            }

            return returnString; // Ids separated by ';'
        }

        // "Pretty print"
        public override string ToString()
        {
            var returnString = "";

            foreach (var e in Events)
            {
                returnString += e.NameOfActivity + " ";
            }
            
            return returnString;
        }
    }
}
