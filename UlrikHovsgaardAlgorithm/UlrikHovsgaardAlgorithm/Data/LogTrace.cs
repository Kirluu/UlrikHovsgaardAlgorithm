using System.Collections.Generic;

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
