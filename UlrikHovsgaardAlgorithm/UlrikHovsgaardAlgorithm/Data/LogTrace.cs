using System.Collections.Generic;
using System.Linq;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class LogTrace
    {
        public string Id { get; set; }
        public readonly List<LogEvent> Events = new List<LogEvent>();

        public LogTrace()
        {
        }

        public LogTrace(params char[] ids)
        {
            this.AddEventsWithChars(ids);
        }

        public void AddEventsWithChars(params char[] ids)
        {
            foreach (var id in ids)
            {
                Add(new LogEvent(id + "", "somename" + id) );
            }
        }

        public void Add(LogEvent e)
        {
            Events.Add(e);
        }

        public LogTrace Copy()
        {
            var copy = new LogTrace();
            foreach (var logEvent in Events)
            {
                copy.Add(new LogEvent(logEvent.IdOfActivity, logEvent.Name)
                {
                    EventId = logEvent.EventId,
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

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var logEvent in Events)
                {
                    hash = hash * 23 + logEvent.GetHashCode();
                }
                return hash;
            }
        }

        // String representation
        public string ToStringForm()
        {
            var returnString = "";
            var firstIte = true;

            foreach (var e in Events)
            {
                returnString += (firstIte ? "" : ";") + e.IdOfActivity;
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
                returnString += e.IdOfActivity + "; ";
            }
            
            return returnString;
        }
    }
}
