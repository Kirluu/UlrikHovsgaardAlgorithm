using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class LogTrace : INotifyPropertyChanged
    {
        public event Action EventAdded;

        private string _id;
        public string Id { get { return _id; } set { _id = value; OnPropertyChanged(); } }
        public readonly List<LogEvent> Events = new List<LogEvent>();
        private bool _isFinished;
        public bool IsFinished { get { return _isFinished; } set { _isFinished = value; OnPropertyChanged(); } }

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
            EventAdded?.Invoke();
        }

        public void Add(LogEvent e)
        {
            Events.Add(e);
            EventAdded?.Invoke();
        }

        public LogTrace Copy()
        {
            var copy = new LogTrace { Id = Id, IsFinished = IsFinished };
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

            var first = true;
            foreach (var e in Events)
            {
                returnString += first ? e.IdOfActivity : "; " + e.IdOfActivity;
                first = false;
            }
            
            return returnString;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        //[NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
