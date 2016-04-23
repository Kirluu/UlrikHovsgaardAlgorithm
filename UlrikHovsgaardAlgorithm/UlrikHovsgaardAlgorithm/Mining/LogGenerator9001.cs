using System;
using System.Collections.Generic;
using System.IO;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Mining
{
    public class LogGenerator9001
    {
        // from 10-100.  determines the chance that the trace will terminate, when possible. a lower index leads to longer traces.
        private readonly int _terminationIndex;
        private readonly DcrGraph _inputGraph;
        private Random _rnd;
        

        public LogGenerator9001 (int terminationIndex, DcrGraph inputGraph)
        {
            if (terminationIndex < 10)
            {
                terminationIndex = 10;
            }

            _terminationIndex = terminationIndex;
            _inputGraph = inputGraph;
            _rnd = new Random();
        }

        public LogTrace TraceGenerator()
        {
            var trace = new LogTrace();
            trace.Id = Guid.NewGuid().ToString();

            //reset the graph.
            DcrGraph graph = _inputGraph.Copy();
            graph.Running = true;

            var id = 0;
            while (true)
            {

                var runnables = graph.GetRunnableActivities();

                if (runnables.Count == 0)
                    break;
                //run a random activity and add it to the log.
                trace.Add(new LogEvent(RunRandomActivity(runnables, graph), "somename") {EventId = id++.ToString()});

                //if we can stop and 
                if(graph.IsFinalState() && _rnd.Next(100) < _terminationIndex)
                    break;

                if (trace.Events.Count >= 10000)
                {
                    throw new InvalidDataException("The graph is unhealthy, so it is not possible to generate a log.");
                }
            }
            trace.IsFinished = true;
            return trace;

        }

        private String RunRandomActivity(HashSet<Activity> set, DcrGraph graph)
        {
            var next = _rnd.Next(set.Count);

            var i = 0;

            foreach (var act in set)
            {
                if (i++ != next) continue;
                graph.Execute(act);
                return act.Id;
            }

            return "";

        }

        public List<LogTrace> GenerateLog(int noOfTraces)
        {
            var log = new List<LogTrace>();

            while (noOfTraces-- > 0)
            {
                log.Add(TraceGenerator());
            }

            return log;
        }

    }
}