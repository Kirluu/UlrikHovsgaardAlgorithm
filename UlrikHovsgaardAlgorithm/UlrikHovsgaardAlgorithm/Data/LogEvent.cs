using System;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class LogEvent
    {
        public string EventId { get; set; } //unique for each event
        public readonly string IdOfActivity; //matches an activity
        public readonly string Name;
        public DateTime TimeOfExecution { get; set; }
        public string ActorName { get; set; }
        public string RoleName { get; set; }

        public LogEvent(string activityId, string name)
        {
            IdOfActivity = activityId;
            Name = name;
        }

        public LogEvent Copy()
        {
            return new LogEvent(IdOfActivity, Name)
            {
                TimeOfExecution = TimeOfExecution,
                ActorName = ActorName,
                RoleName = RoleName
            };
        }

        public override bool Equals(object obj)
        {
            LogEvent otherEvent = obj as LogEvent;
            if (otherEvent != null)
            {
                //TODO: should probably compare on EventId
                //return EventId.Equals(otherEvent.EventId);
                return IdOfActivity == otherEvent.IdOfActivity; // && Name == otherEvent.Name;
            }
            else
            {
                throw new ArgumentException();
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + IdOfActivity.GetHashCode();
                //hash = hash * 23 + Name.GetHashCode();
                return hash;
            }
        }
    }
}
