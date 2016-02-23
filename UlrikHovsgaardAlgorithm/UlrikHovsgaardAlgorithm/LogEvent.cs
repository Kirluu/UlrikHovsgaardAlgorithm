using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class LogEvent
    {
        public string Id { get; set; }
        public string NameOfActivity { get; set; }
        public DateTime TimeOfExecution { get; set; }
        public string ActorName { get; set; }
        public string RoleName { get; set; }

        public override bool Equals(object obj)
        {
            LogEvent otherEvent = obj as LogEvent;
            if (otherEvent != null)
            {
                return this.Id.Equals(otherEvent.Id);
            }
            else
            {
                throw new ArgumentException();
            }

        }
    }
}
