using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm.Datamodels
{
    public static class Threshold
    {
        /// <summary>
        /// An event that occurs when the Threshold value is updated.
        /// Sends along the previous threshold value.
        /// </summary>
        public static event Action<double> ThresholdUpdated;

        private static double _value;
        public static double Value {
            get { return _value; }
            set
            {
                var old = _value;
                _value = value;
                ThresholdUpdated?.Invoke(old);
            }
        }
    }
}
