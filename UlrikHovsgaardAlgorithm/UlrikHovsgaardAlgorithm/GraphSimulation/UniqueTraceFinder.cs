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
        
        private HashSet<ComparableList<int>> _uniqueTraceSet = new HashSet<ComparableList<int>>();

        private HashSet<ComparableList<int>> _compareSet;
        private HashSet<byte[]> _seenStates = new HashSet<byte[]>(new ByteArrayComparer());
        
        private bool _comparisonResult = true;
        
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

        public HashSet<ComparableList<int>> GetUniqueTraces(ByteDcrGraph graph)
        {
            ResetValues();

            GetUniqueTraces(graph, new ComparableList<int>());
            return _uniqueTraceSet;
        }

        public bool CompareTraces(ByteDcrGraph graph, HashSet<ComparableList<int>> compareSet)
        {
            ResetValues();

            _compareSet = compareSet;
            GetUniqueTraces(graph, new ComparableList<int>());

            return _comparisonResult && (_uniqueTraceSet.Count == _compareSet.Count);
        }

        private void ResetValues()
        {
            _compareSet = null;
            _comparisonResult = true;
            _uniqueTraceSet = new HashSet<ComparableList<int>>();
            _seenStates = new HashSet<byte[]>(new ByteArrayComparer());
        }
        

        private void GetUniqueTraces(ByteDcrGraph inputGraph, ComparableList<int> currentTrace)
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

                    _uniqueTraceSet.Add(currentTraceCopy);

                    if(_compareSet != null &&
                        (!_compareSet.Contains(currentTraceCopy)))
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
        
    }
}
