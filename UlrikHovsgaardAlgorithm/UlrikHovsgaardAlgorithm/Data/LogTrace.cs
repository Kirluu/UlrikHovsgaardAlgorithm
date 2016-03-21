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

        public LogTrace(params char[] ids)
        {
            Events = new List<LogEvent>();
            this.AddEventsWithChars(ids);
        }

        public void AddEventsWithChars(params char[] ids)
        {

            foreach (var id in ids)
            {
                Add(new LogEvent() {IdOfActivity = ""+id} );
            }
            
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
                    EventId = logEvent.EventId,
                    IdOfActivity = logEvent.IdOfActivity,
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
                returnString += e.IdOfActivity + " ";
            }
            
            return returnString;
        }
    }
}
