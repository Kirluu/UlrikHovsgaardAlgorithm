using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class QualityDimensions
    {
        public decimal Fitness { get; set; }
        public decimal Precision { get; set; }
        public double Generalization { get; set; }
        public decimal Simplicity { get; set; }
    }
}
