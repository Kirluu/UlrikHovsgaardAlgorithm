using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Utils;

namespace UlrikHovsgaardAlgorithm.GraphSimulation
{
    public class UniqueTraceFinder
    {
        #region Fields
        
        private HashSet<LogTrace> _uniqueTraceSet = new HashSet<LogTrace>();
        private Dictionary<string, Dictionary<byte[], int>> _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();

        public HashSet<LogTrace> TracesToBeComparedToSet { get; } 
        private bool _comparisonResult = true;

        private readonly object _lockObject = new object();
        private ConcurrentQueue<Task> _threads = new ConcurrentQueue<Task>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        #endregion
        
        public UniqueTraceFinder(DcrGraph graph)
        {
            TracesToBeComparedToSet = GetUniqueTracesThreaded(graph);
        }

        #region Primary methods

        // Threading attempt
        public HashSet<LogTrace> GetUniqueTracesThreaded(DcrGraph inputGraph)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Start from scratch
            _uniqueTraceSet = new HashSet<LogTrace>();
            _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();
            _threads = new ConcurrentQueue<Task>();

            var task = Task.Factory.StartNew(() => FindUniqueTracesThreaded(new LogTrace(), inputGraph, false), _cancellationTokenSource.Token);
            _threads.Enqueue(task);

            while (!_threads.IsEmpty)
            {
                Task outThread;
                _threads.TryDequeue(out outThread);
                if (!outThread.IsCanceled)
                {
                    outThread.Wait();
                }
            }

            //Console.WriteLine("-----Start-----");
            Console.WriteLine("Unique Traces Threaded (Set): " + _uniqueTraceSet.Count + ". Elapsed: " + stopwatch.Elapsed);
            //foreach (var logTrace in _uniqueTraceSet)
            //{
            //    foreach (var logEvent in logTrace.Events)
            //    {
            //        Console.Write(logEvent.IdOfActivity);
            //    }
            //    Console.WriteLine();
            //}
            //Console.WriteLine("------End------");
            return _uniqueTraceSet;
        }

        // Threading attempt
        public bool CompareTracesFoundWithSuppliedThreaded(DcrGraph inputGraph)
        {
            if (TracesToBeComparedToSet == null)
            {
                throw new Exception("You must first supply traces to be compared to.");
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Start from scratch
            _uniqueTraceSet = new HashSet<LogTrace>();
            _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();
            _cancellationTokenSource = new CancellationTokenSource();
            _threads = new ConcurrentQueue<Task>();

            _comparisonResult = true;

            // Potentially discover that the found traces do not corrspond, altering _comparisonResult to false
            var task = Task.Factory.StartNew(() => FindUniqueTracesThreaded(new LogTrace(), inputGraph, true), _cancellationTokenSource.Token);
            _threads.Enqueue(task);

            while (!_threads.IsEmpty)
            {
                Task outThread;
                _threads.TryDequeue(out outThread);
                if (!outThread.IsCanceled)
                {
                    try
                    {
                        outThread.Wait();
                    }
                    catch
                    {
                        int i = 0;
                        i++;
                    }
                }
            }

            if (_uniqueTraceSet.Count != TracesToBeComparedToSet.Count)
            {
                _comparisonResult = false;
            }

#if DEBUG
            //Console.WriteLine("-----Start-----");
            Console.WriteLine("Unique Traces With Comparison Threaded (Set): " + _uniqueTraceSet.Count + ". Elapsed: " + stopwatch.Elapsed);
            //foreach (var logTrace in _uniqueTraceSet)
            //{
            //    foreach (var logEvent in logTrace.Events)
            //    {
            //        Console.Write(logEvent.IdOfActivity);
            //    }
            //    Console.WriteLine();
            //}
            //Console.WriteLine("------End------");
#endif

            return _comparisonResult;
        }
        
        private void FindUniqueTracesThreaded(LogTrace currentTrace, DcrGraph inputGraph, bool compareTraces)
        {
            var activitiesToRun = inputGraph.GetRunnableActivities();
            var iterations = new List<Tuple<LogTrace, DcrGraph>>();

            foreach (var activity in activitiesToRun)
            {
                // Create copies
                var inputGraphCopy = inputGraph.Copy();
                var traceCopy = currentTrace.Copy();

                // Record execution
                inputGraphCopy.Running = true;
                inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));
                traceCopy.Events.Add(new LogEvent(activity.Id, activity.Name));

                // Update collections
                lock (_lockObject)
                {
                    AddToAllStatesForTraces(currentTrace, traceCopy, inputGraphCopy);
                }

                if (true) //TODO: extra test for om vi tillader noget forkert. Test at vi ikke erklærer relationer for redundante, som ikke er det.
                // Nothing is pending and included at the same time --> Valid new trace
                {
                    lock (_lockObject)
                    {
                        _uniqueTraceSet.Add(traceCopy);
                    }
                    // IF
                    if (compareTraces // we should compare traces
                        && // AND
                        (!TracesToBeComparedToSet.Any(x => x.Equals(traceCopy)))) // the trace doesn't exist in the set being compared to)
                    {
                        // THEN
                        // One inconsistent trace found - thus not all unique traces are equal
                        lock (_lockObject)
                        {
                            _comparisonResult = false;

                            _cancellationTokenSource.Cancel(); // Cancels all current threads
                        }
                        return;
                    }
                }
                
                lock (_lockObject)
                {
                    if (!IsStateSeenTwiceBeforeInTrace(traceCopy, inputGraphCopy))
                    {
                        // Register wish to continue
                        iterations.Add(new Tuple<LogTrace, DcrGraph>(traceCopy, inputGraphCopy));
                    }
                }
            }

            // For each case where we want to go deeper, recurse
            foreach (var iteration in iterations)
            {
                var localIteration = iteration;
                var task = Task.Factory.StartNew(() => FindUniqueTracesThreaded(localIteration.Item1, localIteration.Item2, compareTraces), _cancellationTokenSource.Token);
                lock (_lockObject)
                {
                    _threads.Enqueue(task);
                }
            }
        }

        #endregion

        #region Helper management methods

        private void AddToAllStatesForTraces(LogTrace currentTrace, LogTrace newTrace, DcrGraph newGraph)
        {
            var newDcrState = DcrGraph.HashDcrGraph(newGraph);
            Dictionary<byte[], int> prevStates;
            if (_allStatesForTraces.TryGetValue(currentTrace.ToStringForm(), out prevStates)) // Trace before execution - any states seen?
            {
                var clonedDictionary = new Dictionary<byte[], int>(prevStates, new ByteArrayComparer());

                // Search clonedDictionary for already exisiting state
                if (clonedDictionary.ContainsKey(newDcrState))
                {
                    // Increase count for this state
                    clonedDictionary[newDcrState] += 1;
                }
                else
                {
                    // Add first occurance of new state
                    clonedDictionary.Add(newDcrState, 1);
                }
                // Update outer dictionary with this trace's states
                _allStatesForTraces.Add(newTrace.ToStringForm(), clonedDictionary); // NewTrace --> PrevStates + NewState
            }
            else
            {
                // Add for current trace (post-exec)
                _allStatesForTraces.Add(newTrace.ToStringForm(), new Dictionary<byte[], int>(new ByteArrayComparer()) { { newDcrState, 1 } });
            }
        }

        private bool IsStateSeenTwiceBeforeInTrace(LogTrace trace, DcrGraph graph)
        {
            Dictionary<byte[], int> traceStates;
            if (_allStatesForTraces.TryGetValue(trace.ToStringForm(), out traceStates))
            {
                int count;
                return traceStates.TryGetValue(DcrGraph.HashDcrGraph(graph), out count) && count > 1;
            }
            throw new Exception("Whoops! Seems you didn't correctly add the states for your current trace! :&");
        }

        #endregion
    }
}
