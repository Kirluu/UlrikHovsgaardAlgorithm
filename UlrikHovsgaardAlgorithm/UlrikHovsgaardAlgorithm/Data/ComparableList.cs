using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class ComparableList<T> : List<T>
    {
        public ComparableList() : base()
        {
        } 

        public ComparableList(ComparableList<T> items) : base(items)
        {
        }

        public override bool Equals(object obj)
        {
            var otherList = obj as ComparableList<T>;
            if (otherList == null || Count != otherList.Count)
            {
                return false;
            }
            return !this.Where((t, i) => !t.Equals(otherList[i])).Any();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return this.Aggregate(19, (current, item) => current * 31 + item.GetHashCode());
            }
        }

        public override string ToString()
        {
            return this.Aggregate("", (current, elem) => current + (elem.ToString()));
        }
    }
}
