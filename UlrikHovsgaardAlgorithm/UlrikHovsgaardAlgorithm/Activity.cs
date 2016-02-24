using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class Activity
    {
        public readonly string Id;
        public readonly string Name;
        public bool Included { get; set; }
        public bool Executed { get; set; }
        public bool Pending { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

        public Activity(string id, string name)
        {
            Id = id;
            Name = name;
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

        //public override int GetHashCode()
        //{
        //    unchecked
        //    {
        //        int hash = 17;
        //        // Suitable nullity checks etc, of course :)
        //        hash = hash * 23 + Id.GetHashCode();
        //        hash = hash * 23 + Name.GetHashCode();
        //        return hash;
        //    }
        //}
    }
}
