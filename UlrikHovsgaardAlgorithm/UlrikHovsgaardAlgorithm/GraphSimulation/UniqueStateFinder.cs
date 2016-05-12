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
        public static int Counter;

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

            //FindUniqueStatesInclRunnableActivityCount(inputGraph);
            //FindUniqueStatesInclRunnableActivityCountDepthFirst(inputGraph);
            FindUniqueStatesInclRunnableActivityCountDepthFirstBytes(new ByteDcrGraph(inputGraph));

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
            Counter++;
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

        private static void FindUniqueStatesInclRunnableActivityCountDepthFirstBytes(ByteDcrGraph inputGraph)
        {
            Counter++;
            var activitiesToRun = inputGraph.GetRunnableIndexes();

            var clone = new byte[inputGraph.State.Length];
            inputGraph.State.CopyTo(clone, 0);
            _seenStatesWithRunnableActivityCount.Add(clone, activitiesToRun.Count);
            
            foreach (var activityIdx in activitiesToRun)
            {
                // Spawn new work
                var inputGraphCopy = new ByteDcrGraph(inputGraph);
                inputGraphCopy.ExecuteActivity(activityIdx);

                var stateSeen = _seenStatesWithRunnableActivityCount.ContainsKey(inputGraphCopy.State);

                if (!stateSeen)
                {
                    // Register wish to continue
                    FindUniqueStatesInclRunnableActivityCountDepthFirstBytes(inputGraphCopy);
                }
            }
        }

        private static void FindUniqueStatesInclRunnableActivityCountDepthFirst(DcrGraph inputGraph)
        {
            Counter++;
            var activitiesToRun = inputGraph.GetRunnableActivities();

            var hashed = DcrGraph.HashDcrGraph(inputGraph);
             _seenStatesWithRunnableActivityCount.Add(hashed, activitiesToRun.Select(x => x.Id).ToList().Count);
            

            foreach (var activity in activitiesToRun)
            {
                // Spawn new work
                var inputGraphCopy = inputGraph.Copy();
                inputGraphCopy.Running = true;
                inputGraphCopy.Execute(inputGraphCopy.GetActivity(activity.Id));

                var stateSeen = _seenStatesWithRunnableActivityCount.ContainsKey(DcrGraph.HashDcrGraph(inputGraphCopy));

                if (!stateSeen)
                {
                    // Register wish to continue
                    FindUniqueStatesInclRunnableActivityCountDepthFirst(inputGraphCopy);
                }
            }
        }
    }
}
