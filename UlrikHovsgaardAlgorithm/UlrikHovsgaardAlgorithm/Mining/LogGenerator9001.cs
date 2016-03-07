using System;
using System.Collections.Generic;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Mining
{
    public class LogGenerator9001
    {
        // from 0-100. Default 40. determines the chance that the trace will terminate, when possible. a lower index leads to longer traces.
        private readonly int _terminationIndex;
        private readonly DcrGraph _inputGraph;
        private DcrGraph _graph;
        

        public LogGenerator9001 (int terminationIndex, DcrGraph inputGraph)
        {
            _terminationIndex = terminationIndex;
            _inputGraph = inputGraph;
        }

        public LogTrace TraceGenerator()
        {
            var trace = new LogTrace();

            //reset the graph.
            _graph = _inputGraph.Copy2();
            _graph.Running = true;

            var id = 0;
            while (true)
            {

                var runnables = _graph.GetRunnableActivities();

                if (runnables.Count == 0)
                    break;
                //run a random activity and add it to the log.
                trace.Add(new LogEvent() {Id = id++.ToString(), NameOfActivity = RunRandomActivity(runnables)});

                //if we can stop and 
                if(_graph.IsFinalState() && new Random().Next(100) < _terminationIndex)
                    break;
            }
            return trace;

        }

        private String RunRandomActivity(HashSet<Activity> set)
        {

            var next = new Random().Next(set.Count);

            var i = 0;

            foreach (var act in set)
            {
                if (i++ != next) continue;
                _graph.Execute(act);
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