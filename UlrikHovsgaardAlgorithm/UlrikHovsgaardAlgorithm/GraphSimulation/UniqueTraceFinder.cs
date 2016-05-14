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
        private HashSet<byte[]> _seenStates;
        private HashSet<byte[]> _compareStates; //saves which activities can be run.
        private HashSet<ComparableList<int>> _compareSet;
        
        private bool _comparisonResult = true;
        
        #endregion

        public UniqueTraceFinder(ByteDcrGraph graph)
        {
            SetUniqueTraces(graph);
        }

        private HashSet<ComparableList<int>> SetUniqueTraces(ByteDcrGraph graph)
        {
            ResetValues();

            FindUniqueTraces(graph, new ComparableList<int>());
            _compareSet = _uniqueTraceSet;
            _compareStates = new HashSet<byte[]>(_seenStates.Select(graph.StateWithNonRunnableActivitiesEqual), new ByteArrayComparer());
            //_compareStates = _seenStates;
            return _uniqueTraceSet;
        }

        public bool CompareTraces(ByteDcrGraph graph)
        {
            ResetValues();
            FindUniqueTraces(graph, new ComparableList<int>());

            return _comparisonResult && (_uniqueTraceSet.Count == _compareSet.Count);
        }

        private void ResetValues()
        {
            _comparisonResult = true;
            _uniqueTraceSet = new HashSet<ComparableList<int>>();

            _seenStates = new HashSet<byte[]>(new ByteArrayComparer());
        }
        
        

        private void FindUniqueTraces(ByteDcrGraph inputGraph, ComparableList<int> currentTrace)
        {
            //compare trace length with desired depth
            foreach (var activity in inputGraph.GetRunnableIndexes())
            {
                if (currentTrace.Count == 100)
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
                if (!_seenStates.Contains(inputGraphCopy.State))
                {
                    if (_compareStates != null
                        && !_compareStates.Contains(inputGraphCopy.StateWithNonRunnableActivitiesEqual(inputGraphCopy.State)))
                    {
                        _comparisonResult = false;
                        return;
                    }
                    //var newStates = new HashSet<Byte[]>(seenStates, new ByteArrayComparer());
                    //newStates.Add(inputGraphCopy.State);
                    _seenStates.Add(inputGraphCopy.State);
                    FindUniqueTraces(inputGraphCopy,currentTraceCopy);
                }
            }
        }
        
    }
}
