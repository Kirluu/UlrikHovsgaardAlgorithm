using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class Activity
    {
        public readonly string Id;
        public readonly string Name;
        public bool Included { get; set; }
        public bool Executed { get; set; }
        public bool Pending { get; set; }
        public readonly bool IsNestedGraph;
        public List<string> Roles { get; set; } = new List<string>();
        private DcrGraph _nestedGraph;


        public Activity(string id, string name)
        {
            var regex = new Regex("^[\\w ]+$");
            if (regex.IsMatch(id) == false)
            {
                throw new ArgumentException("The ID value provided must consist of only unicode letters and numbers and spaces.");
            }
            Id = id;
            Name = name;
            IsNestedGraph = false;
        }

        public Activity(string id, string name, DcrGraph nestedDcrGraph)
        {
            var regex = new Regex("^[\\w ]+$");
            if (regex.IsMatch(id) == false)
            {
                throw new ArgumentException("The ID value provided must consist of only unicode letters and numbers and spaces.");
            }
            Id = id;
            Name = name;
            IsNestedGraph = true;
            _nestedGraph = nestedDcrGraph;
        }

        public Activity Copy()
        {
            return new Activity(this.Id, this.Name) { Roles = this.Roles, Executed = this.Executed, Included = this.Included, Pending = this.Pending };
        }

        public override bool Equals(object obj)
        {
            Activity otherActivity = obj as Activity;
            if (otherActivity != null)
            {
                return this.Id.Equals(otherActivity.Id);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + Id.GetHashCode();
                hash = hash * 23 + Name.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (IsNestedGraph)
                return _nestedGraph.Activities.Aggregate("Nested graph activities: \n", (current,a) => current + "\t" + a+ "\n");

            return Id + " : " + Name + " inc=" + Included + ", pnd=" + Pending + ", exe=" + Executed;
        }

    }
}
