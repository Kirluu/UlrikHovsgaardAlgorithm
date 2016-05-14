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
        
        private bool _comparisonResult = true;
        
        #endregion
        
        public HashSet<ComparableList<int>> GetUniqueTraces(ByteDcrGraph graph)
        {
            ResetValues();

            GetUniqueTraces(graph, new ComparableList<int>(), new HashSet<byte[]>());
            return _uniqueTraceSet;
        }

        public bool CompareTraces(ByteDcrGraph graph, HashSet<ComparableList<int>> compareSet)
        {
            ResetValues();

            _compareSet = compareSet;
            GetUniqueTraces(graph, new ComparableList<int>(), new HashSet<byte[]>(new ByteArrayComparer()));

            return _comparisonResult && (_uniqueTraceSet.Count == _compareSet.Count);
        }

        private void ResetValues()
        {
            _compareSet = null;
            _comparisonResult = true;
            _uniqueTraceSet = new HashSet<ComparableList<int>>();
        }
        

        private void GetUniqueTraces(ByteDcrGraph inputGraph, ComparableList<int> currentTrace, HashSet<byte[]> seenStates)
        {
            
            //compare trace length with desired depth
            foreach (var activity in inputGraph.GetRunnableIndexes())
            {
                if (currentTrace.Count == 0)
                {
                    var test = 252354;
                }

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
                if (!seenStates.Contains(inputGraphCopy.State))
                {
                    var newStates = new HashSet<Byte[]>(seenStates, new ByteArrayComparer());
                    newStates.Add(inputGraphCopy.State);
                    GetUniqueTraces(inputGraphCopy,currentTraceCopy,newStates);
                }
            }
        }
        
    }
}
