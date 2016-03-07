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



        public override string ToString()
        {
            var returnString = "";

            foreach (var e in Events)
            {
                returnString += e.NameOfActivity + " ";
            }
            ;
            return returnString;
        }
    }
}
