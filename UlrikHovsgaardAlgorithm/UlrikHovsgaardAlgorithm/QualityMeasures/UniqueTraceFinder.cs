using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Utils;

namespace UlrikHovsgaardAlgorithm.QualityMeasures
{
    public class UniqueTraceFinder
    {
        #region Fields
        
        private HashSet<ComparableList<int>> _uniqueTraceSet = new HashSet<ComparableList<int>>();
        private HashSet<ComparableList<int>> _uniqueEarlyTerminationTraceSet = new HashSet<ComparableList<int>>();
        private HashSet<byte[]> _seenStates;
        private HashSet<byte[]> _compareStates;
        private HashSet<ComparableList<int>> _compareTraceSet;
        private HashSet<ComparableList<int>> _compareEarlyTerminationTraceSet;
        private ByteDcrGraph _compareByteGraph;
        
        private bool _comparisonResult = true;
        public List<string> ComparisonFailureTrace { get; private set; } // Failure-trace as list of strings
        
        #endregion

        public UniqueTraceFinder(ByteDcrGraph graph)
        {
            _compareByteGraph = graph.Copy();
            SetUniqueTraces(graph);
        }

        public bool IsNoAcceptingTrace()
        {
            return _compareTraceSet.Count == 0;
        }

        public List<List<string>> GetLanguageAsListOfTracesWithIds()
        {
            return _compareTraceSet.Select(traceInts =>
                traceInts.Select(index =>
                _compareByteGraph.IndexToActivityId[index]) // Look up Ids from indexes
                .ToList()).ToList();
        }

        private HashSet<ComparableList<int>> SetUniqueTraces(ByteDcrGraph graph)
        {
            ResetValues();

            FindUniqueTraces(graph, new ComparableList<int>());
            _compareTraceSet = _uniqueTraceSet;
            _compareEarlyTerminationTraceSet = _uniqueEarlyTerminationTraceSet;
            _compareStates = new HashSet<byte[]>(_seenStates.Select(ByteDcrGraph.StateWithExcludedActivitiesEqual), new ByteArrayComparer());

            return _uniqueTraceSet;
        }

        public bool CompareTraces(ByteDcrGraph graph)
        {
            ResetValues();
            FindUniqueTraces(graph, new ComparableList<int>());

            /* NOTE: State-space comparison is NOT a valid comparison-factor, since **different** graphs 
               may represent the same graph language. */
            return _comparisonResult && _uniqueTraceSet.Count == _compareTraceSet.Count
                   && _compareEarlyTerminationTraceSet.Count == _uniqueEarlyTerminationTraceSet.Count
                   && _compareEarlyTerminationTraceSet.Union(_uniqueEarlyTerminationTraceSet).Count() == _uniqueEarlyTerminationTraceSet.Count;
        }

        private void ResetValues()
        {
            _comparisonResult = true;
            ComparisonFailureTrace = null;
            _uniqueTraceSet = new HashSet<ComparableList<int>>();
            _uniqueEarlyTerminationTraceSet = new HashSet<ComparableList<int>>();

            _seenStates = new HashSet<byte[]>(new ByteArrayComparer());
        }
        
        
        // TODO: Use while loop instead
        private void FindUniqueTraces(ByteDcrGraph inputGraph, ComparableList<int> currentTrace)
        {
            // TODO: Replace if not working:
            //FindUniqueTracesNonRecursive(inputGraph);
            //return;

            //compare trace length with desired depth
            foreach (var activity in inputGraph.GetRunnableIndexes())
            {
                var inputGraphCopy = inputGraph.Copy();
                var currentTraceCopy = new ComparableList<int>(currentTrace);
                
                // Execute and add event to trace
                inputGraphCopy.ExecuteActivity(activity);
                currentTraceCopy.Add(activity);

                if (ByteDcrGraph.IsFinalState(inputGraphCopy.State))
                {
                    _uniqueTraceSet.Add(currentTraceCopy);

                    if(_compareTraceSet != null &&
                        (!_compareTraceSet.Contains(currentTraceCopy)))
                    {
                        _comparisonResult = false;
                        ComparisonFailureTrace = currentTraceCopy.Select(x => inputGraphCopy.IndexToActivityId[x]).ToList();
                        return;
                    }
                }

                // If we have not seen the state before (successfull ADD it to the state)
                //if (_seenStates.Add(ByteDcrGraph.StateWithExcludedActivitiesEqual(inputGraphCopy.State))) // Doc: "returns false if already present" // THIS GIVES FAULTY RESULTS - MUST CONSIDER ALL POSSIBLE STATES TO ENSURE FULL LANGUAGE-DISCOVERY
                if (_seenStates.Add(inputGraphCopy.State)) // Doc: "returns false if already present"
                {
                    //if (_compareStates != null
                    //    && !_compareStates.Contains(inputGraphCopy.StateWithNonRunnableActivitiesEqual(inputGraphCopy.State)))
                    //{
                    //    _comparisonResult = false;
                    //    return;
                    //}

                    FindUniqueTraces(inputGraphCopy, currentTraceCopy);
                }
                else // State has already been seen:
                {
                    // Add to collection of traces that reached previously seen states through different, alternate paths
                    _uniqueEarlyTerminationTraceSet.Add(currentTraceCopy);

                    // If we found an alternate path that the original graph semantics trace-finding could not, we have observed a change
                    if (_compareEarlyTerminationTraceSet != null &&
                        !_compareEarlyTerminationTraceSet.Contains(currentTraceCopy))
                    {
                        // TODO: Terminate early allowed here? We reached a seen state in a way that the original trace-finding did not
                    }
                }
            }
        }

        private void FindUniqueTracesNonRecursive(ByteDcrGraph inputGraph)
        {
            // Keep a queue of the upcoming activities to execute from a certain state
            var q = new Queue<(int, ByteDcrGraph, ComparableList<int>)>();
            var initRunnable = inputGraph.GetRunnableIndexes();
            initRunnable.ForEach(runnable => q.Enqueue((runnable, inputGraph, new ComparableList<int>())));

            while (q.Count > 0)
            {
                var (activityToExecute, stateToExecuteIn, currentTrace) = q.Dequeue();
                var newState = stateToExecuteIn.Copy();
                var newTrace = new ComparableList<int>(currentTrace);

                newState.ExecuteActivity(activityToExecute);

                newTrace.Add(activityToExecute);

                if (ByteDcrGraph.IsFinalState(newState.State))
                {
                    _uniqueTraceSet.Add(newTrace);

                    if (newTrace[0] == 0)
                    {
                        var i = 0;
                    }

                    if (_compareTraceSet != null &&
                        !_compareTraceSet.Contains(newTrace))
                    {
                        _comparisonResult = false;
                        return;
                    }
                }

                // If we have not seen the state before
                if (!_seenStates.Contains(newState.State))
                {
                    _seenStates.Add(newState.State);

                    // Instead of recursing here, we expand the exploration-queue
                    var newRunnables = newState.GetRunnableIndexes();
                    newRunnables.ForEach(runnable => q.Enqueue((runnable, newState, new ComparableList<int>())));
                }
                else
                {
                    // Add to collection of traces that reached previously seen states through different, alternate paths
                    _uniqueEarlyTerminationTraceSet.Add(newTrace);

                    // If we found an alternate path that the original graph semantics trace-finding could not, we have observed a change
                    //if (_compareEarlyTerminationTraceSet != null &&
                    //    !_compareEarlyTerminationTraceSet.Contains(currentTraceCopy))
                    //{
                    //    // TODO: Terminate early allowed here? We reached a seen state in a way that the original trace-finding did not
                    //}
                }
            }
        }

    }
}
