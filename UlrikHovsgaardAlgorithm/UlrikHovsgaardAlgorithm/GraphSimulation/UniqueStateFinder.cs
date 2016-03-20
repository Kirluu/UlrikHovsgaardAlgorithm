using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.GraphSimulation
{
    public static class UniqueStateFinder
    {
        private static List<DcrGraph> _seenStates;
        private static Dictionary<byte[], List<string>> _seenStatesWithRunnableActivities; 

        public static List<DcrGraph> GetUniqueStates(DcrGraph inputGraph)
        {
            // Start from scratch
            _seenStates = new List<DcrGraph>();

            FindUniqueStates(inputGraph);

            return _seenStates;
        }

        //public static Dictionary<byte[], List<string>> GetUniqueStatesWithRunnableActivities(DcrGraph inputGraph)
        //{
        //    // Start from scratch
        //    _seenStatesWithRunnableActivities = new Dictionary<byte[], List<string>>();

        //    FindUniqueStatesInclRunnableActivities(inputGraph);

        //    return _seenStatesWithRunnableActivities;
        //}

        private static void FindUniqueStates(DcrGraph inputGraph)
        {
            var activitiesToRun = inputGraph.GetRunnableActivities();
            var iterations = new List<DcrGraph>();

            _seenStates.Add(inputGraph);

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
                FindUniqueStates(unseenState); // TODO: Spawn thread
            }
        }

        //private static void FindUniqueStatesInclRunnableActivities(DcrGraph inputGraph)
        //{
        //    var activitiesToRun = inputGraph.GetRunnableActivities();
        //    var iterations = new List<DcrGraph>();

        //    _seenStates.Add(inputGraph);
        //    _seenStatesWithRunnableActivities.Add(DcrGraph.HashDcrGraph(inputGraph), activitiesToRun.Select(x => x.Id).ToList());

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
    }
}
