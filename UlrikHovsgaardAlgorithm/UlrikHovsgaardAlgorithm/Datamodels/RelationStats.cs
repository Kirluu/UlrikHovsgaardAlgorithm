using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm.Datamodels
{
    public class RelationStats
    {
        /// <summary>
        /// An event that occurs whenever a relation passes the current threshold.
        /// Passes 'true' if Confidence raised above the threshold, 'false' if it fell below the threshold.
        /// </summary>
        public event Action<bool> PassedThreshold;

        public double Confidence { get { return Violations / Invocations; } }
        public int Violations { get; private set; }
        public int Invocations { get; private set; }

        public RelationStats(bool violationOccurred)
        {
            // Initialize
            if (violationOccurred)
                Violations++;
            Invocations++;

            // Listen to Threshold
            Threshold.ThresholdUpdated += OnThresholdUpdated;
        }

        public void Invoke(bool violationOccurred)
        {
            var oldC = Confidence;
            if (violationOccurred)
                Violations++;
            Invocations++;
            var newC = Confidence;

            // Check whether the confidence's update makes it go past the threshold
            var t = Threshold.Value;
            if (oldC < t && t < newC) // Went above the threshold
                PassedThreshold?.Invoke(true);
            else if (newC < t && t < oldC) // Fell below the threshold
                PassedThreshold?.Invoke(false);
        }

        private void OnThresholdUpdated(double oldThreshold)
        {
            var c = Confidence;
            var newT = Threshold.Value;
            var oldT = oldThreshold;

            // Check whether the threshold's update makes it go past the confidence
            if (oldT < c && c < newT)
                PassedThreshold?.Invoke(false); // Threshold raised above confidence --> Confidence lower than threshold
            else if (newT < c && c < oldT)
                PassedThreshold?.Invoke(true); // Threshold fell below confidence --> Confidence higher than threshold
        }
    }
}
