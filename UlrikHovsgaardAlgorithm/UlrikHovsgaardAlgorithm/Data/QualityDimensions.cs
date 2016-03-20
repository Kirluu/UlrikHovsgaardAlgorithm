using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class QualityDimensions
    {
        public double Fitness { get; set; }
        public double Precision { get; set; }
        public double Generalization { get; set; }
        public double Simplicity { get; set; }

        public override string ToString()
        {
            return "Fitness:\t\t" + Fitness + " % \nPrecision:\t\t" + Precision + " % \nGeneralization:\t\t" + Generalization +
                   " % \nSimplicity:\t\t" + Simplicity + " %";
        }
    }
}
