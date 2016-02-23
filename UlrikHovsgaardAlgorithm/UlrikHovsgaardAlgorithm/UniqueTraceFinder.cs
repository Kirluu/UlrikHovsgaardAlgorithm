using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class UniqueTraceFinder
    {
        private List<LogTrace> _uniqueTraces = new List<LogTrace>();
        private List<DcrGraph> _seenStates = new List<DcrGraph>();

        public List<LogTrace> GetUniqueTraces(DcrGraph inputGraph)
        {
            FindUniqueTraces(new LogTrace {Events = new List<LogEvent>()}, inputGraph);

            return _uniqueTraces;
        } 

        private void FindUniqueTraces(LogTrace currentTrace, DcrGraph inputGraph)
        {
            var activitiesToRun = inputGraph.GetRunnableActivities();
            
            _seenStates.Add(inputGraph);

            foreach (var activity in activitiesToRun)
            {
                // Spawn new work
                var copy = inputGraph.Copy();
                copy.Execute(activity);
                currentTrace.Events.Add(new LogEvent { Id = activity.Id });

                if (copy.IsStoppable()) // Nothing is pending and included at the same time --> Valid new trace
                {
                    //_uniqueTraces.Add(currentTrace);

                    // Add unique trace if unique (checking just to be sure... - may not be needed) TODO: Verify need (can just add without check?)
                    foreach (var uniqueTrace in _uniqueTraces)
                    {
                        var diff1 = uniqueTrace.Events.Except(currentTrace.Events);
                        var diff2 = currentTrace.Events.Except(uniqueTrace.Events);
                        if (diff1.Any() || diff2.Any())
                        {
                            _uniqueTraces.Add(currentTrace);
                            break;
                        }
                    }
                }

                // If state seen before, do not explore further
                foreach (var seenState in _seenStates)
                {
                    if (seenState.AreInEqualState(copy))
                    {
                        // If the state after execution of 'activity' has been seen before, we are not interested in this trace TODO: Verify logic
                        return;
                    }
                }

                // Continue deepening
                FindUniqueTraces(currentTrace, copy);
            }

            // TODO: Use DCR graph and find all possible unique traces
            // TODO: Consider cycles... (if same state is met, don't explore further)
        }
    }
}
