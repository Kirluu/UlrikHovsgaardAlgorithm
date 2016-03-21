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
        public Dictionary<Activity, int> ActivityRunCount { get; } = new Dictionary<Activity, int>();
        public Dictionary<Activity, Dictionary<Activity, int>> IncludeRelationWitnessMatrix { get; } = new Dictionary<Activity, Dictionary<Activity, int>>();
        public Dictionary<Activity, Dictionary<Activity, double>> IncludeRelationTrustMatrix { get; } = new Dictionary<Activity, Dictionary<Activity, double>>();

        #region Fields

        private readonly Log _inputLog;
        private readonly int _lookBackFactor;

        #endregion

        public StatisticsRetriever(Log inputLog)
        {
            _inputLog = inputLog;
            // Initialize properties
            Alphabet = new HashSet<Activity>(inputLog.Alphabet.Select(x => new Activity(x.IdOfActivity, x.Name)).ToList());
            foreach (var source in Alphabet)
            {
                ActivityRunCount.Add(source, 0);
                IncludeRelationWitnessMatrix.Add(source, new Dictionary<Activity, int>());
                IncludeRelationTrustMatrix.Add(source, new Dictionary<Activity, double>());
                foreach (var target in Alphabet)
                {
                    IncludeRelationWitnessMatrix[source].Add(target, 0);
                    IncludeRelationTrustMatrix[source].Add(target, 0.0);
                }
            }
            var decidingFactor = Alphabet.Count / 4; // TODO: Revise
            _lookBackFactor = decidingFactor < 2 ? 2 : (decidingFactor > 5 ? decidingFactor : 5); // Minimum 2, maximum 5 TODO: Revise
        }

        public Dictionary<Activity, Dictionary<Activity, double>> RetrieveIncludeRelationTrust()
        {
            foreach (var trace in _inputLog.Traces)
            {
                var priorEvents = new Queue<LogEvent>(_lookBackFactor);
                
                foreach (var logEvent in trace.Events)
                {
                    // Register that activity occured
                    ActivityRunCount[new Activity(logEvent.IdOfActivity, logEvent.Name)]++;
                    if (priorEvents.Any())
                    {
                        foreach (var priorEvent in priorEvents)
                        {
                            // Register that current 'logEvent' occured after the prior events
                            IncludeRelationWitnessMatrix[new Activity(priorEvent.IdOfActivity, priorEvent.Name)][
                            new Activity(logEvent.IdOfActivity, logEvent.Name)]++;
                        }
                    }

                    // Manage/Update recent events e.g. _lookBackFactor = 3: pEvents = ABC, logEvent = A --> BCA
                    if (priorEvents.Count == _lookBackFactor)
                    {
                        priorEvents.Dequeue();
                    }
                    priorEvents.Enqueue(logEvent);
                }
            }

            // Build result
            foreach (var keyVal in IncludeRelationWitnessMatrix)
            {
                var source = keyVal.Key;
                foreach (var keyValInner in keyVal.Value)
                {
                    var target = keyValInner.Key;
                    var witnessCount = keyValInner.Value;
                    // Number of times run after divided by the amount of times source was run at all
                    IncludeRelationTrustMatrix[source][target] = (double) witnessCount / ActivityRunCount[source];
                }
            }

            return IncludeRelationTrustMatrix;
        }
    }
}
