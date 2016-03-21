using System;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class LogEvent
    {
        
        public string EventId { get; set; } //unique for each event
        public string IdOfActivity { get; set; } //matches an activity
        public string Name { get; set; }
        public DateTime TimeOfExecution { get; set; }
        public string ActorName { get; set; }
        public string RoleName { get; set; }

        public override bool Equals(object obj)
        {
            LogEvent otherEvent = obj as LogEvent;
            if (otherEvent != null)
            {
                //TODO: should probably compare on EventId
                //return EventId.Equals(otherEvent.EventId);
                return IdOfActivity == otherEvent.IdOfActivity;
            }
            else
            {
                throw new ArgumentException();
            }

        }
    }
}
