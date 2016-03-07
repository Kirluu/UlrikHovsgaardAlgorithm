using System;
using System.Collections.Generic;
using System.Linq;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.RedundancyRemoval
{
    public class UniqueTraceFinderWithComparison
    {
        #region Fields

        private static readonly Object Obj = new Object();

        private List<LogTrace> _uniqueTraces = new List<LogTrace>();
        private List<DcrGraph> _seenStates = new List<DcrGraph>();

        private List<LogTrace> _tracesToBeComparedTo;
        private bool _comparisonResult = true;

        #endregion

        #region Primary methods

        public void SupplyTracesToBeComparedTo(List<LogTrace> tracesToBeComparedTo)
        {
            _tracesToBeComparedTo = tracesToBeComparedTo;
        }

        #region Working version with regular recursion

        // WORKING
        public List<LogTrace> GetUniqueTraces(DcrGraph inputGraph)
        {
            // Start from scratch
            _uniqueTraces = new List<LogTrace>();
            _seenStates = new List<DcrGraph>();

            FindUniqueTraces(new LogTrace { Events = new List<LogEvent>() }, inputGraph, false);

#if DEBUG
            Console.WriteLine("-----Start-----");
            foreach (var logTrace in _uniqueTraces)
            {
                foreach (var logEvent in logTrace.Events)
                {
                    Console.Write(logEvent.Id);
                }
                Console.WriteLine();
            }
            Console.WriteLine("------End------");
#endif

            return _uniqueTraces;
        }

        // WORKING
        public bool CompareTracesFoundWithSupplied(DcrGraph inputGraph)
        {
            if (_tracesToBeComparedTo == null)
            {
                throw new Exception("You must first supply traces to be compared to.");
            }

            // Start from scratch
            _uniqueTraces = new List<LogTrace>();
            _seenStates = new List<DcrGraph>();

            _comparisonResult = true;

            // Potentially discover that the found traces do not corrspond, altering _comparisonResult to false
            FindUniqueTraces(new LogTrace { Events = new List<LogEvent>() }, inputGraph, true);

#if DEBUG
            Console.WriteLine("-----Start-----");
            foreach (var logTrace in _uniqueTraces)
            {
                foreach (var logEvent in logTrace.Events)
                {
                    Console.Write(logEvent.Id);
                }
                Console.WriteLine();
            }
            Console.WriteLine("------End------");
#endif

            return _comparisonResult;
        }

        // WORKING
        private void FindUniqueTraces(LogTrace currentTrace, DcrGraph inputGraph, bool compareTraces)
        {
            var activitiesToRun = inputGraph.GetRunnableActivities();
            var iterations = new List<Tuple<LogTrace, DcrGraph>>();

            _seenStates.Add(inputGraph);

            foreach (var activity in activitiesToRun)
            {
                // Spawn new work
                var inputGraphCopy = inputGraph.Copy2();
                var traceCopy = CopyLogTrace(currentTrace);
                inputGraphCopy.Running = true;
                inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));
                traceCopy.Events.Add(new LogEvent { Id = activity.Id, NameOfActivity = activity.Name });

                if (inputGraphCopy.IsFinalState())
                // Nothing is pending and included at the same time --> Valid new trace
                {
                    var currentTraceIndex = _uniqueTraces.Count;
                    _uniqueTraces.Add(traceCopy);

                    // Perform comparison of this trace with same-index trace of compared trace list
                    if (compareTraces // Whether to compare traces or not
                        &&
                        // TODO: Consider if less traces found than in _tracesToBeComparedTo
                        (currentTraceIndex >= _tracesToBeComparedTo.Count // More traces found than the amount being compared to
                            || (currentTraceIndex < _tracesToBeComparedTo.Count
                                && !AreTracesEqualSingle(traceCopy, _tracesToBeComparedTo[currentTraceIndex])))) // The traces are unequal
                    {
                        // One inconsistent trace found - thus not all unique traces are equal
                        _comparisonResult = false;
                        return; // Stops all recursion
                    }
                }

                // If state seen before, do not explore further
                var stateSeen = _seenStates.Any(seenState => seenState.AreInEqualState(inputGraphCopy));
                if (!stateSeen)
                {
                    // Register wish to continue
                    iterations.Add(new Tuple<LogTrace, DcrGraph>(traceCopy, inputGraphCopy));
                }
            }

            // For each case where we want to go deeper, recurse
            for (int i = 0; i < iterations.Count; i++)
            {
                // One of these calls may lead to the call below, ending all execution...
                FindUniqueTraces(iterations[i].Item1, iterations[i].Item2, compareTraces);
            }
        }

        #endregion

        #region Old version with Trampolining - not working

        public List<LogTrace> GetUniqueTraces3(DcrGraph inputGraph)
        {
            FindUniqueTraces3(new LogTrace { Events = new List<LogEvent>() }, inputGraph, false);

            return _uniqueTraces;
        }

        public bool CompareTracesFoundWithSupplied3(DcrGraph inputGraph)
        {
            if (_tracesToBeComparedTo == null)
            {
                throw new Exception("You must first supply traces to be compared to.");
            }

            _comparisonResult = true;

            // Potentially discover that the found traces do not corrspond, altering _comparisonResult to false
            FindUniqueTraces3(new LogTrace { Events = new List<LogEvent>() }, inputGraph, true);

            return _comparisonResult;
        }

        /// <summary>
        /// Finds traces based on the inputGraph and compares a found trace with the TracesToBeComparedTo, to enable
        /// a quick stop, if something doesn't add up (doesn't amount to the same trace-options)
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="graph"></param>
        /// <param name="compareTraces">A boolean value defining whether or not to compare found traces to those in _tracesToBeComparedTo</param>
        /// <returns></returns>
        private void FindUniqueTraces3(LogTrace trace, DcrGraph graph, bool compareTraces)
        {
            _uniqueTraces = new List<LogTrace>(); // Start from scratch
            _seenStates = new List<DcrGraph>();

            Guid activeWorkerGuid = Guid.Empty;

            var function = Trampoline.MakeActionTrampoline((LogTrace currentTrace, DcrGraph inputGraph) =>
            {
                var activitiesToRun = inputGraph.GetRunnableActivities();
                var iterations = new List<Tuple<LogTrace, DcrGraph>>();

                _seenStates.Add(inputGraph);

                foreach (var activity in activitiesToRun)
                {
                    // Spawn new work
                    var inputGraphCopy = inputGraph.Copy2();
                    var traceCopy = CopyLogTrace(currentTrace);
                    inputGraphCopy.Running = true;
                    inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));
                    traceCopy.Events.Add(new LogEvent { Id = activity.Id, NameOfActivity = activity.Name });

                    if (inputGraphCopy.IsFinalState())
                    // Nothing is pending and included at the same time --> Valid new trace
                    {
                        var currentTraceIndex = _uniqueTraces.Count;
                        _uniqueTraces.Add(traceCopy);

                        // Perform comparison of this trace with same-index trace of compared trace list
                        if (compareTraces // Whether to compare traces or not
                            &&
                            // TODO: Consider if less traces found than in _tracesToBeComparedTo
                            (currentTraceIndex >= _tracesToBeComparedTo.Count // More traces found than the amount being compared to
                                || (currentTraceIndex < _tracesToBeComparedTo.Count
                                    && !AreTracesEqualSingle(traceCopy, _tracesToBeComparedTo[currentTraceIndex])))) // The traces are unequal
                        {
                            // One inconsistent trace found - thus not all unique traces are equal
                            _comparisonResult = false;
                            return Trampoline.EndAction<LogTrace, DcrGraph>(); // Stops all recursion
                        }
                    }

                    // If state seen before, do not explore further
                    var stateSeen = _seenStates.Any(seenState => seenState.AreInEqualState(inputGraphCopy));
                    if (!stateSeen)
                    {
                        // Register wish to continue
                        iterations.Add(new Tuple<LogTrace, DcrGraph>(traceCopy, inputGraphCopy));
                    }
                }

                var ownerId = Guid.NewGuid();

                // For each case where we want to go deeper, recurse
                for (int i = 0; i < iterations.Count; i++)
                {
                    // One of these calls may lead to the call below, ending all execution...
                    return Trampoline.RecurseAction(iterations[i].Item1, iterations[i].Item2);
                }

                return Trampoline.EndAction<LogTrace, DcrGraph>();
            });

            function(trace, graph);
        }

        #endregion

        #endregion

        #region Helper methods

        // TODO: Can be improved if both inputlists (of traces) are sorted by event-ID (or ASSUMPTION)
        public static bool AreUniqueTracesEqual(List<LogTrace> traces1, List<LogTrace> traces2)
        {
            if (traces1.Count != traces2.Count)
            {
                return false;
            }

            #region WITHOUT sorting assumption

            //foreach (var trace1 in traces1)
            //{
            //    bool matchingTraceFound = false;
            //    foreach (var trace2 in traces2)
            //    {
            //        if (AreTracesEqualSingle(trace1, trace2))
            //        {
            //            matchingTraceFound = true;
            //            break;
            //        }
            //    }
            //    if (!matchingTraceFound)
            //    {
            //        return false;
            //    }
            //}

            #endregion

            #region WITH sorting assumption

            // (That two equal sets of unique traces are not discovered in different order, due to using the same discovery method)
            return !traces1.Where((t, i) => !AreTracesEqualSingle(t, traces2[i])).Any();

            #endregion
        }

        public static bool AreTracesEqualSingle(LogTrace trace1, LogTrace trace2)
        {
            if (trace1.Events.Count != trace2.Events.Count)
            {
                return false;
            }
            return !trace1.Events.Where((t, i) => !t.Equals(trace2.Events[i])).Any();
        }

        private LogTrace CopyLogTrace(LogTrace trace)
        {
            var copy = new LogTrace { Events = new List<LogEvent>() };
            foreach (var logEvent in trace.Events)
            {
                copy.Add(new LogEvent
                {
                    Id = logEvent.Id,
                    NameOfActivity = logEvent.NameOfActivity,
                    ActorName = logEvent.ActorName,
                    RoleName = logEvent.RoleName,
                    TimeOfExecution = logEvent.TimeOfExecution
                });
            }
            return copy;
        }

        #endregion
    }
}
