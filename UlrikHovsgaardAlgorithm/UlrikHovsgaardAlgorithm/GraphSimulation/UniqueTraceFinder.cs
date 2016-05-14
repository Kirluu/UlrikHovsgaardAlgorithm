﻿using System;
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
        
        public static HashSet<ComparableList<int>> UniqueTraceSet = new HashSet<ComparableList<int>>();

        public static HashSet<ComparableList<int>> CompareSet;
        private static HashSet<byte[]> _seenStates = new HashSet<byte[]>(new ByteArrayComparer());

        //private Dictionary<string, Dictionary<byte[], int>> _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();

        //public HashSet<LogTrace> TracesToBeComparedToSet { get; } 
        private static bool _comparisonResult = true;

        //private readonly object _lockObject = new object();
        //private ConcurrentQueue<Task> _threads = new ConcurrentQueue<Task>();
        //private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        #endregion
        
        //public UniqueTraceFinder(DcrGraph graph)
        //{
        //    TracesToBeComparedToSet = GetUniqueTracesThreaded(graph);
        //}

        //public UniqueTraceFinder(ByteDcrGraph graph)
        //{
        //    GetUniqueTraces(graph, new List<int>());
        //}

        //public UniqueTraceFinder(ByteDcrGraph graph, HashSet<List<int>> compareHashSet)
        //{
        //    CompareSet = compareHashSet;
        //    GetUniqueTraces(graph, new List<int>());
        //}

        public static HashSet<ComparableList<int>> GetUniqueTraces(ByteDcrGraph graph)
        {
            ResetValues();

            GetUniqueTraces(graph, new ComparableList<int>());
            return UniqueTraceSet;
        }

        public static bool CompareTraces(ByteDcrGraph graph, HashSet<ComparableList<int>> compareSet)
        {
            ResetValues();

            CompareSet = compareSet;
            GetUniqueTraces(graph, new ComparableList<int>());

            return _comparisonResult && (UniqueTraceSet.Count == CompareSet.Count);
        }

        private static void ResetValues()
        {
            CompareSet = null;
            _comparisonResult = true;
            UniqueTraceSet = new HashSet<ComparableList<int>>();
            _seenStates = new HashSet<byte[]>(new ByteArrayComparer());
        }

        #region Primary methods

        private static void GetUniqueTraces(ByteDcrGraph inputGraph, ComparableList<int> currentTrace)
        {
            //compare trace length with desired depth
            foreach (var activity in inputGraph.GetRunnableIndexes())
            {
                var inputGraphCopy = new ByteDcrGraph(inputGraph);
                
                var currentTraceCopy = new ComparableList<int>(currentTrace);

                inputGraphCopy.ExecuteActivity(activity);

                //add event to trace
                currentTraceCopy.Add(activity);

                if (ByteDcrGraph.IsFinalState(inputGraphCopy.State))
                {
                    UniqueTraceSet.Add(currentTraceCopy);

                    if(CompareSet != null &&
                        (!CompareSet.Contains(currentTraceCopy)))
                    {
                        _comparisonResult = false;
                        return;
                    }
                }
                //if we have not seen the state before
                if (!_seenStates.Contains(inputGraphCopy.State))
                {
                    _seenStates.Add(inputGraphCopy.State);
                    GetUniqueTraces(inputGraphCopy,currentTraceCopy);
                }
            }
        }

        //if we have seen the state before


//            }
//            //var stopwatch = new Stopwatch();
//            //stopwatch.Start();

//            //// Start from scratch
//            //_uniqueTraceSet = new HashSet<LogTrace>();
//            //_allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();
//            //_threads = new ConcurrentQueue<Task>();

//            //var task = Task.Factory.StartNew(() => FindUniqueTracesThreadedBytes(new LogTrace(), inputGraph, false), _cancellationTokenSource.Token);
//            //_threads.Enqueue(task);

//            //while (!_threads.IsEmpty)
//            //{
//            //    Task outThread;
//            //    _threads.TryDequeue(out outThread);
//            //    if (!outThread.IsCanceled)
//            //    {
//            //        outThread.Wait();
//            //    }
//            //}

//            //Console.WriteLine("Unique Traces Threaded (Set): " + _uniqueTraceSet.Count + ". Elapsed: " + stopwatch.Elapsed);
//            //return _uniqueTraceSet;
//        }

//        public bool CompareTracesFoundWithSuppliedThreadedBytes(ByteDcrGraph inputGraph)
//        {
//            if (TracesToBeComparedToSet == null)
//            {
//                throw new Exception("You must first supply traces to be compared to.");
//            }

//            var stopwatch = new Stopwatch();
//            stopwatch.Start();

//            // Start from scratch
//            _uniqueTraceSet = new HashSet<LogTrace>();
//            _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();
//            _cancellationTokenSource = new CancellationTokenSource();
//            _threads = new ConcurrentQueue<Task>();

//            _comparisonResult = true;

//            // Potentially discover that the found traces do not corrspond, altering _comparisonResult to false
//            var task = Task.Factory.StartNew(() => FindUniqueTracesThreadedBytes(new LogTrace(), inputGraph, true), _cancellationTokenSource.Token);
//            _threads.Enqueue(task);

//            while (!_threads.IsEmpty)
//            {
//                Task outThread;
//                _threads.TryDequeue(out outThread);
//                if (!outThread.IsCanceled)
//                {
//                    try
//                    {
//                        outThread.Wait();
//                    }
//                    catch
//                    {
//                        // Do nothing... Normally an exception due to cancellation of thread being waited upon
//                    }
//                }
//            }

//            if (_uniqueTraceSet.Count != TracesToBeComparedToSet.Count)
//            {
//                _comparisonResult = false;
//            }

//#if DEBUG
//            Console.WriteLine("Unique Traces With Comparison Threaded (Set): " + _uniqueTraceSet.Count + ". Elapsed: " + stopwatch.Elapsed);
//#endif

//            return _comparisonResult;
                //}

                #region Non-Byte versions

//            public
//            HashSet<LogTrace> GetUniqueTracesThreaded(DcrGraph inputGraph)
//        {
//            var stopwatch = new Stopwatch();
//            stopwatch.Start();
            
//            // Start from scratch
//            _uniqueTraceSet = new HashSet<LogTrace>();
//            _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();
//            _threads = new ConcurrentQueue<Task>();

//            var task = Task.Factory.StartNew(() => FindUniqueTracesThreaded(new LogTrace(), inputGraph, false), _cancellationTokenSource.Token);
//            _threads.Enqueue(task);

//            while (!_threads.IsEmpty)
//            {
//                Task outThread;
//                _threads.TryDequeue(out outThread);
//                if (!outThread.IsCanceled)
//                {
//                    outThread.Wait();
//                }
//            }

//            Console.WriteLine("Unique Traces Threaded (Set): " + _uniqueTraceSet.Count + ". Elapsed: " + stopwatch.Elapsed);
//            return _uniqueTraceSet;
//        }
        
//        public bool CompareTracesFoundWithSuppliedThreaded(DcrGraph inputGraph)
//        {
//            if (TracesToBeComparedToSet == null)
//            {
//                throw new Exception("You must first supply traces to be compared to.");
//            }

//            var stopwatch = new Stopwatch();
//            stopwatch.Start();

//            // Start from scratch
//            _uniqueTraceSet = new HashSet<LogTrace>();
//            _allStatesForTraces = new Dictionary<string, Dictionary<byte[], int>>();
//            _cancellationTokenSource = new CancellationTokenSource();
//            _threads = new ConcurrentQueue<Task>();

//            _comparisonResult = true;

//            // Potentially discover that the found traces do not corrspond, altering _comparisonResult to false
//            //var task = Task.Factory.StartNew(() => FindUniqueTracesThreaded(new LogTrace(), inputGraph, true), _cancellationTokenSource.Token);
//            var task = Task.Factory.StartNew(() => FindUniqueTracesThreadedBytes(new LogTrace(), new ByteDcrGraph(inputGraph), true), _cancellationTokenSource.Token);
//            _threads.Enqueue(task);

//            while (!_threads.IsEmpty)
//            {
//                Task outThread;
//                _threads.TryDequeue(out outThread);
//                if (!outThread.IsCanceled)
//                {
//                    outThread.Wait();
//                }
//            }

//            if (_uniqueTraceSet.Count != TracesToBeComparedToSet.Count)
//            {
//                _comparisonResult = false;
//            }

//#if DEBUG
//            Console.WriteLine("Unique Traces With Comparison Threaded (Set): " + _uniqueTraceSet.Count + ". Elapsed: " + stopwatch.Elapsed);
//#endif

//            return _comparisonResult;
//        }

//        #endregion

//        private void FindUniqueTracesThreadedBytes(LogTrace currentTrace, ByteDcrGraph inputGraph, bool compareTraces)
//        {
//            var activitiesToRun = inputGraph.GetRunnableIndexes();
//            var iterations = new List<Tuple<LogTrace, ByteDcrGraph>>();

//            foreach (var activity in activitiesToRun)
//            {
//                // Create copies
//                var inputGraphCopy = new ByteDcrGraph(inputGraph);
//                var traceCopy = currentTrace.Copy();

//                // Record execution
//                inputGraphCopy.ExecuteActivity(activity);
//                traceCopy.Events.Add(new LogEvent(activity.ToString(), activity.ToString()));

//                // Update collections
//                lock (_lockObject)
//                {
//                    AddToAllStatesForTracesBytes(currentTrace, traceCopy, inputGraphCopy);
//                }

//                if (ByteDcrGraph.IsFinalState(inputGraphCopy.State))
//                // Nothing is pending and included at the same time --> Valid new trace
//                {
//                    lock (_lockObject)
//                    {
//                        _uniqueTraceSet.Add(traceCopy);
//                    }
//                    // IF
//                    if (compareTraces // we should compare traces
//                        && // AND
//                        (!TracesToBeComparedToSet.Any(x => x.Equals(traceCopy)))) // the trace doesn't exist in the set being compared to)
//                    {
//                        // THEN
//                        // One inconsistent trace found - thus not all unique traces are equal
//                        lock (_lockObject)
//                        {
//                            _comparisonResult = false;

//                            _cancellationTokenSource.Cancel(); // Cancels all current threads
//                        }
//                        return;
//                    }
//                }

//                lock (_lockObject)
//                {
//                    if (!IsStateSeenTwiceBeforeInTraceBytes(traceCopy, inputGraphCopy))
//                    {
//                        // Register wish to continue
//                        iterations.Add(new Tuple<LogTrace, ByteDcrGraph>(traceCopy, inputGraphCopy));
//                    }
//                }
//            }

//            // For each case where we want to go deeper, recurse
//            foreach (var iteration in iterations)
//            {
//                var localIteration = iteration;
//                if (_threads.Count >= 8)
//                {
//                    FindUniqueTracesThreadedBytes(localIteration.Item1, localIteration.Item2, compareTraces);
//                }
//                else
//                {
//                    var task = Task.Factory.StartNew(() => FindUniqueTracesThreadedBytes(localIteration.Item1, localIteration.Item2, compareTraces), _cancellationTokenSource.Token);
//                    lock (_lockObject)
//                    {
//                        _threads.Enqueue(task);
//                    }
//                }
//            }
//        }

//        #region Non-byte version

//        private void FindUniqueTracesThreaded(LogTrace currentTrace, DcrGraph inputGraph, bool compareTraces)
//        {
//            var activitiesToRun = inputGraph.GetRunnableActivities();
//            var iterations = new List<Tuple<LogTrace, DcrGraph>>();

//            foreach (var activity in activitiesToRun)
//            {
//                // Create copies
//                var inputGraphCopy = inputGraph.Copy();
//                var traceCopy = currentTrace.Copy();

//                // Record execution
//                inputGraphCopy.Running = true;
//                inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));
//                traceCopy.Events.Add(new LogEvent(activity.Id, activity.Name));

//                // Update collections
//                lock (_lockObject)
//                {
//                    AddToAllStatesForTraces(currentTrace, traceCopy, inputGraphCopy);
//                }

//                if (inputGraphCopy.IsFinalState()) //TODO: extra test for om vi tillader noget forkert. Test at vi ikke erklærer relationer for redundante, som ikke er det.
//                // Nothing is pending and included at the same time --> Valid new trace
//                {
//                    lock (_lockObject)
//                    {
//                        _uniqueTraceSet.Add(traceCopy);
//                    }
//                    // IF
//                    if (compareTraces // we should compare traces
//                        && // AND
//                        (!TracesToBeComparedToSet.Any(x => x.Equals(traceCopy)))) // the trace doesn't exist in the set being compared to)
//                    {
//                        // THEN
//                        // One inconsistent trace found - thus not all unique traces are equal
//                        lock (_lockObject)
//                        {
//                            _comparisonResult = false;

//                            _cancellationTokenSource.Cancel(); // Cancels all current threads
//                        }
//                        return;
//                    }
//                }
                
//                lock (_lockObject)
//                {
//                    if (!IsStateSeenTwiceBeforeInTrace(traceCopy, inputGraphCopy))
//                    {
//                        // Register wish to continue
//                        iterations.Add(new Tuple<LogTrace, DcrGraph>(traceCopy, inputGraphCopy));
//                    }
//                }
//            }

//            // For each case where we want to go deeper, recurse
//            foreach (var iteration in iterations)
//            {
//                var localIteration = iteration;
//                if (_threads.Count >= 8)
//                {
//                    FindUniqueTracesThreaded(localIteration.Item1, localIteration.Item2, compareTraces);
//                }
//                else
//                {
//                    var task = Task.Factory.StartNew(() => FindUniqueTracesThreaded(localIteration.Item1, localIteration.Item2, compareTraces), _cancellationTokenSource.Token);
//                    lock (_lockObject)
//                    {
//                        _threads.Enqueue(task);
//                    }
//                }
//            }
//        }

        #endregion

        #endregion

        //#region Helper management methods

        //private void AddToAllStatesForTracesBytes(LogTrace currentTrace, LogTrace newTrace, ByteDcrGraph newGraph)
        //{
        //    Dictionary<byte[], int> prevStates;
        //    if (_allStatesForTraces.TryGetValue(currentTrace.ToStringForm(), out prevStates)) // Trace before execution - any states seen?
        //    {
        //        var clonedDictionary = new Dictionary<byte[], int>(prevStates, new ByteArrayComparer());

        //        // Search clonedDictionary for already exisiting state
        //        if (clonedDictionary.ContainsKey(newGraph.State))
        //        {
        //            // Increase count for this state
        //            clonedDictionary[newGraph.State] += 1;
        //        }
        //        else
        //        {
        //            // Add first occurance of new state
        //            clonedDictionary.Add(newGraph.State, 1);
        //        }
        //        // Update outer dictionary with this trace's states
        //        _allStatesForTraces.Add(newTrace.ToStringForm(), clonedDictionary); // NewTrace --> PrevStates + NewState
        //    }
        //    else
        //    {
        //        // Add for current trace (post-exec)
        //        _allStatesForTraces.Add(newTrace.ToStringForm(), new Dictionary<byte[], int>(new ByteArrayComparer()) { { newGraph.State, 1 } });
        //    }
        //}

        //private bool IsStateSeenTwiceBeforeInTraceBytes(LogTrace trace, ByteDcrGraph graph)
        //{
        //    Dictionary<byte[], int> traceStates;
        //    if (_allStatesForTraces.TryGetValue(trace.ToStringForm(), out traceStates))
        //    {
        //        int count;
        //        return traceStates.TryGetValue(graph.State, out count) && count > 1;
        //    }
        //    throw new Exception("Whoops! Seems you didn't correctly add the states for your current trace! :&");
        //}

        //#region Non-byte versions

        //private void AddToAllStatesForTraces(LogTrace currentTrace, LogTrace newTrace, DcrGraph newGraph)
        //{
        //    var newDcrState = DcrGraph.HashDcrGraph(newGraph);
        //    Dictionary<byte[], int> prevStates;
        //    if (_allStatesForTraces.TryGetValue(currentTrace.ToStringForm(), out prevStates)) // Trace before execution - any states seen?
        //    {
        //        var clonedDictionary = new Dictionary<byte[], int>(prevStates, new ByteArrayComparer());

        //        // Search clonedDictionary for already exisiting state
        //        if (clonedDictionary.ContainsKey(newDcrState))
        //        {
        //            // Increase count for this state
        //            clonedDictionary[newDcrState] += 1;
        //        }
        //        else
        //        {
        //            // Add first occurance of new state
        //            clonedDictionary.Add(newDcrState, 1);
        //        }
        //        // Update outer dictionary with this trace's states
        //        _allStatesForTraces.Add(newTrace.ToStringForm(), clonedDictionary); // NewTrace --> PrevStates + NewState
        //    }
        //    else
        //    {
        //        // Add for current trace (post-exec)
        //        _allStatesForTraces.Add(newTrace.ToStringForm(), new Dictionary<byte[], int>(new ByteArrayComparer()) { { newDcrState, 1 } });
        //    }
        //}

        //private bool IsStateSeenTwiceBeforeInTrace(LogTrace trace, DcrGraph graph)
        //{
        //    Dictionary<byte[], int> traceStates;
        //    if (_allStatesForTraces.TryGetValue(trace.ToStringForm(), out traceStates))
        //    {
        //        int count;
        //        return traceStates.TryGetValue(DcrGraph.HashDcrGraph(graph), out count) && count > 1;
        //    }
        //    throw new Exception("Whoops! Seems you didn't correctly add the states for your current trace! :&");
        //}

        //#endregion

        //#endregion
    }
}
