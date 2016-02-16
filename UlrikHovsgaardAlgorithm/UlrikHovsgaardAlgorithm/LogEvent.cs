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
        public DateTime TimeOfExecution { get; set; }
        public string ActorName { get; set; }
        public string RoleName { get; set; }
    }
}
