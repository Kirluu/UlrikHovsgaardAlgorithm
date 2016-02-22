using System;
using System.Collections.Generic;

namespace UlrikHovsgaardAlgorithm
{
    public class LogGenerator9001
    {
        // from 0-100. Default 40. determines the chance that the trace will terminate, when possible. a lower index leads to longer traces.
        private int terminationIndex = 40;
        

        public LogGenerator9001 (int terminationIndex, DcrGraph inputGraph)
        {
            this.terminationIndex = terminationIndex;
        }

        public LogTrace TraceGenerator()
        {
            throw new NotImplementedException();    
        }

        public List<LogTrace> GenerateLog(int noOfTraces)
        {
            List<LogTrace> log = new List<LogTrace>();

            while (noOfTraces-- > 0)
            {
                log.Add(TraceGenerator());
            }

            return log;
        }
    }
}