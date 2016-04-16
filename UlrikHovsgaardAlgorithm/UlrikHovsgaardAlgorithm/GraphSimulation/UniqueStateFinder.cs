using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Utils;

namespace UlrikHovsgaardAlgorithm.GraphSimulation
{
    public static class UniqueStateFinder
    {
        private static List<DcrGraph> _seenStates;
        private static Dictionary<byte[], int> _seenStatesWithRunnableActivityCount; 

        //public static List<DcrGraph> GetUniqueStates(DcrGraph inputGraph)
        //{
        //    // Start from scratch
        //    _seenStates = new List<DcrGraph>();

        //    FindUniqueStates(inputGraph);

        //    return _seenStates;
        //}

        public static Dictionary<byte[], int> GetUniqueStatesWithRunnableActivityCount(DcrGraph inputGraph)
        {
            // Start from scratch
            _seenStates = new List<DcrGraph>();
            _seenStatesWithRunnableActivityCount = new Dictionary<byte[], int>(new ByteArrayComparer());

            FindUniqueStatesInclRunnableActivityCount(inputGraph);

            return _seenStatesWithRunnableActivityCount;
        }

        //private static void FindUniqueStates(DcrGraph inputGraph)
        //{
        //    var activitiesToRun = inputGraph.GetRunnableActivities();
        //    var iterations = new List<DcrGraph>();

        //    _seenStates.Add(inputGraph);

        //    foreach (var activity in activitiesToRun)
        //    {
        //        // Spawn new work
        //        var inputGraphCopy = inputGraph.Copy();
        //        inputGraphCopy.Running = true;
        //        inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));
                
        //        var stateSeen = _seenStates.Any(seenState => seenState.AreInEqualState(inputGraphCopy));

        //        if (!stateSeen)
        //        {
        //            // Register wish to continue
        //            iterations.Add(inputGraphCopy);
        //        }
        //    }

        //    // For each case where we want to go deeper, recurse
        //    foreach (var unseenState in iterations)
        //    {
        //        FindUniqueStates(unseenState); // TODO: Spawn thread
        //    }
        //}

        private static void FindUniqueStatesInclRunnableActivityCount(DcrGraph inputGraph)
        {
            var activitiesToRun = inputGraph.GetRunnableActivities();
            var iterations = new List<DcrGraph>();

            _seenStates.Add(inputGraph);

            var hashed = DcrGraph.HashDcrGraph(inputGraph);
            if (! _seenStatesWithRunnableActivityCount.ContainsKey(hashed))
            {
                _seenStatesWithRunnableActivityCount.Add(hashed, activitiesToRun.Select(x => x.Id).ToList().Count);
            }

            foreach (var activity in activitiesToRun)
            {
                // Spawn new work
                var inputGraphCopy = inputGraph.Copy();
                inputGraphCopy.Running = true;
                inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));

                var stateSeen = _seenStates.Any(seenState => seenState.AreInEqualState(inputGraphCopy));

                if (!stateSeen)
                {
                    // Register wish to continue
                    iterations.Add(inputGraphCopy);
                }
            }

            // For each case where we want to go deeper, recurse
            foreach (var unseenState in iterations)
            {
                FindUniqueStatesInclRunnableActivityCount(unseenState); // TODO: Spawn thread
            }
        }
    }
}
