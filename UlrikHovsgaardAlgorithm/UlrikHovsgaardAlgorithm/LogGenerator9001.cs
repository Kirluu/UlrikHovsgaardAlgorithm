using System;
using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm
{
    public class LogGenerator9001
    {
        // from 0-100. Default 40. determines the chance that the trace will terminate, when possible. a lower index leads to longer traces.
        private int terminationIndex = 40;
        //how many traces to generate
        private int noOfTraces;
        

        public LogGenerator9001 (int index, DcrGraph inputGraph)
        {
            terminationIndex = index;
        }


        public List<LogEvent> traceGenerator()
        {
            throw new NotImplementedException();    
        } 



    }
}