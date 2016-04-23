using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Utils
{
    public class RelationCouple
    {
        public readonly Activity Activity1;
        public readonly Activity Activity2;

        public RelationCouple(Activity a1, Activity a2)
        {
            Activity1 = a1.Copy();
            Activity2 = a2.Copy();
        }

        public override bool Equals(object obj)
        {
            var otherCouple = obj as RelationCouple;
            if (otherCouple != null)
            {
                // Either (1 == 1 && 2 == 2) or (1 == 2 and 2 == 1)
                return (Activity1.Id == otherCouple.Activity1.Id && Activity2.Id == otherCouple.Activity2.Id)
                    || (Activity1.Id == otherCouple.Activity2.Id && Activity2.Id == otherCouple.Activity1.Id);
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
                hash = hash * 23 + Activity1.Id.GetHashCode() + Activity2.Id.GetHashCode();
                return hash;
            }
        }
    }
}
