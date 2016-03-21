using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Mining
{
    public class StatisticsRetriever
    {
        public HashSet<Activity> Alphabet { get; }
        public Dictionary<Activity, Dictionary<Activity, double>> RelationMatrix { get; } = new Dictionary<Activity, Dictionary<Activity, double>>();

        #region Fields

        private Log _inputLog;

        #endregion

        public StatisticsRetriever(Log inputLog)
        {
            _inputLog = inputLog;
            // Initialize properties
            Alphabet = new HashSet<Activity>(inputLog.Alphabet.Select(x => new Activity(x.IdOfActivity, x.Name)).ToList());
            foreach (var source in Alphabet)
            {
                RelationMatrix.Add(source, new Dictionary<Activity, double>());
                foreach (var target in Alphabet)
                {
                    RelationMatrix[source].Add(target, 0.0);
                }
            }
        }

        public Dictionary<Activity, Dictionary<Activity, double>> Retrieve()
        {
            foreach (var trace in _inputLog.Traces)
            {
                foreach (var logEvent in trace.Events)
                {
                    
                }
            }


            return RelationMatrix;
        }
    }
}
