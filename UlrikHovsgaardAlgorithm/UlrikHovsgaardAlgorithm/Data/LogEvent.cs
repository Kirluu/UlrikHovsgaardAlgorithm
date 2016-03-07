using System;

namespace UlrikHovsgaardAlgorithm.Data
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
                //return Id.Equals(otherEvent.Id);
                return Id == otherEvent.Id;
            }
            else
            {
                throw new ArgumentException();
            }

        }
    }
}
