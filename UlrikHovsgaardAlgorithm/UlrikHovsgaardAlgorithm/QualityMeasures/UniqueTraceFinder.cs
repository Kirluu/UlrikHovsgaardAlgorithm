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
        private Dictionary<byte[], bool> _seenStates; // Stores each seen state along with whether or not it has lead to an accepting trace (accepting state)
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

            FindUniqueTraces(graph, new ComparableList<int>(), new List<byte[]> { graph.State });
            _compareTraceSet = _uniqueTraceSet;
            _compareEarlyTerminationTraceSet = _uniqueEarlyTerminationTraceSet;
            _compareStates = new HashSet<byte[]>(_seenStates.Select(x => ByteDcrGraph.StateWithExcludedActivitiesEqual(x.Key)), new ByteArrayComparer());

            return _uniqueTraceSet;
        }

        public bool CompareTraces(ByteDcrGraph graph)
        {
            ResetValues();
            FindUniqueTraces(graph, new ComparableList<int>(), new List<byte[]> { graph.State });

            /* NOTE: State-space comparison is NOT a valid comparison-factor, since **different** graphs 
               may represent the same graph language. */
            return _comparisonResult && _uniqueTraceSet.Count == _compareTraceSet.Count
                   && _compareEarlyTerminationTraceSet.Count == _uniqueEarlyTerminationTraceSet.Count
                   && _compareEarlyTerminationTraceSet.SetEquals(_uniqueEarlyTerminationTraceSet);
        }

        private void ResetValues()
        {
            _comparisonResult = true;
            ComparisonFailureTrace = null;
            _uniqueTraceSet = new HashSet<ComparableList<int>>();
            _uniqueEarlyTerminationTraceSet = new HashSet<ComparableList<int>>();

            _seenStates = new Dictionary<byte[], bool>(new ByteArrayComparer());
        }
        
        
        /// <summary>
        /// Private function used to discover the full language of a DCR graph in the shape of its memory-optimized ByteDcrGraph format.
        /// </summary>
        /// <param name="inputGraph">The ByteDcrGraph for which we wish to discover the language.</param>
        /// <param name="currentTrace">A comparable list of integers, signifying the activity-IDs given in
        /// the 'inputGraph' in an order signifying a trace of activity-executions.</param>
        /// <param name="statesSeenInTrace">A list of states that are meant to be updated as 'leading to an accepting trace/state later on'
        /// if the state is not a final state itself. When a state is learned to have lead to such an accepting state later on, we stop passing
        /// it around (removing them from the list to save memory).</param>
        private void FindUniqueTraces(ByteDcrGraph inputGraph, ComparableList<int> currentTrace, List<byte[]> statesSeenInTrace)
        {
            // TODO: Make work, since it avoids potential stack-overflow (bounded by memory instead):
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

                var isFinalState = ByteDcrGraph.IsFinalState(inputGraphCopy.State);
                if (isFinalState)
                {
                    // Store this trace as unique, accepting trace
                    _uniqueTraceSet.Add(currentTraceCopy);

                    // Store the fact that all the states seen until here, lead to some accepting trace:
                    // NOTE: **Intended**: Only set true for the states PRIOR to the change we've just seen (by activity execution)
                    // NOTE: ^--> We handle the (non-)occurrence of whether the new state reached was seen before below
                    foreach (var state in statesSeenInTrace)
                    {
                        _seenStates[state] = true; // The states seen prior in trace all lead to a final state via the language
                    }
                    // Optimization: We can disregard holding all of these states for this trace now, because we already said they all lead to acceptance
                    statesSeenInTrace.Clear(); // Clears across the remaining DFS branches too - if one path leads to acceptance, we don't need to re-update for every other path to acceptance too

                    if (_compareTraceSet != null &&
                        (!_compareTraceSet.Contains(currentTraceCopy)))
                    {
                        _comparisonResult = false;
                        ComparisonFailureTrace = currentTraceCopy.Select(x => inputGraphCopy.IndexToActivityId[x]).ToList();
                        return;
                    }
                }

                // If we have seen the state before
                //if (_seenStates.Add(ByteDcrGraph.StateWithExcludedActivitiesEqual(inputGraphCopy.State))) // Doc: "returns false if already present" // THIS GIVES FAULTY RESULTS - MUST CONSIDER ALL POSSIBLE STATES TO ENSURE FULL LANGUAGE-DISCOVERY
                if (_seenStates.TryGetValue(inputGraphCopy.State, out var leadsToAcceptingState))
                {
                    /* ASSUMPTION: When checking for previously having seen our newly reached state,
                     * the "leadsToAcceptingState" value is fully updated and dependable due to D-F-S. */
                    if (leadsToAcceptingState)
                    {
                        // Add to collection of traces that reached previously seen states through different, alternate paths
                        _uniqueEarlyTerminationTraceSet.Add(currentTraceCopy);

                        /* If we found an alternate path to a previously seen state (which leads to an accepting trace),
                         * that the original graph semantics trace-finding could not: We have observed a language change */
                        if (_compareEarlyTerminationTraceSet != null &&
                            !_compareEarlyTerminationTraceSet.Contains(currentTraceCopy))
                        {
                            _comparisonResult = false;
                            ComparisonFailureTrace = currentTraceCopy.Select(x => inputGraphCopy.IndexToActivityId[x]).ToList();
                            return;
                        }
                    }
                }
                else // State not seen before:
                {
                    /* Perform the first observation of this newly reached state along with the local knowledge of whether it leads to a final state,
                     * determined by whether it itself is one such final state */
                    _seenStates.Add(inputGraphCopy.State, isFinalState);

                    // Add newly reached state, because it's not a final state itself, so we will have to update later if we reach a final state later
                    var statesSeenInTraceCopy = new List<byte[]>(statesSeenInTrace);
                    statesSeenInTraceCopy.Add(inputGraphCopy.State);

                    // RECURSION:
                    /* Optimization-note: We pass on the original, mutable list of states seen prior in trace to 
                     * allow future accepting states found to optimize the list through updates even further backwards in the DFS flow. */
                    FindUniqueTraces(inputGraphCopy, currentTraceCopy, isFinalState ? statesSeenInTrace : statesSeenInTraceCopy);
                }
            }
        }

        //private void FindUniqueTracesNonRecursive(ByteDcrGraph inputGraph)
        //{
        //    // Keep a queue of the upcoming activities to execute from a certain state
        //    var q = new Queue<(int, ByteDcrGraph, ComparableList<int>)>();
        //    var initRunnable = inputGraph.GetRunnableIndexes();
        //    initRunnable.ForEach(runnable => q.Enqueue((runnable, inputGraph, new ComparableList<int>())));

        //    while (q.Count > 0)
        //    {
        //        var (activityToExecute, stateToExecuteIn, currentTrace) = q.Dequeue();
        //        var newState = stateToExecuteIn.Copy();
        //        var newTrace = new ComparableList<int>(currentTrace);

        //        newState.ExecuteActivity(activityToExecute);

        //        newTrace.Add(activityToExecute);

        //        var isFinalState = ByteDcrGraph.IsFinalState(newState.State);
        //        if (isFinalState)
        //        {
        //            _uniqueTraceSet.Add(newTrace);

        //            if (newTrace[0] == 0)
        //            {
        //                var i = 0;
        //            }

        //            if (_compareTraceSet != null &&
        //                !_compareTraceSet.Contains(newTrace))
        //            {
        //                _comparisonResult = false;
        //                return;
        //            }
        //        }

        //        // If we have not seen the state before
        //        if (!_seenStates.Contains(newState.State))
        //        {
        //            _seenStates.Add(newState.State);

        //            // Instead of recursing here, we expand the exploration-queue
        //            var newRunnables = newState.GetRunnableIndexes();
        //            newRunnables.ForEach(runnable => q.Enqueue((runnable, newState, new ComparableList<int>())));
        //        }
        //        else // TODO: Only store alternate paths to already seen state if that state has lead to a final-state eventually
        //        {
        //            // Add to collection of traces that reached previously seen states through different, alternate paths
        //            _uniqueEarlyTerminationTraceSet.Add(newTrace);

        //            // If we found an alternate path that the original graph semantics trace-finding could not, we have observed a change
        //            //if (_compareEarlyTerminationTraceSet != null &&
        //            //    !_compareEarlyTerminationTraceSet.Contains(currentTraceCopy))
        //            //{
        //            //    // TODO: Terminate early allowed here? We reached a seen state in a way that the original trace-finding did not
        //            //}
        //        }
        //    }
        //}

    }
}
