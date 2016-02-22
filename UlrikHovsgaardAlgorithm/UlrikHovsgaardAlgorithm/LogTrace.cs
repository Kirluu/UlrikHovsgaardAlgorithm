using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
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
