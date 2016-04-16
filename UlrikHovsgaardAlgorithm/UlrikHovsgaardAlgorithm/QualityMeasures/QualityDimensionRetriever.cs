using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.GraphSimulation;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardAlgorithm.Utils;

namespace UlrikHovsgaardAlgorithm.QualityMeasures
{
    public static class QualityDimensionRetriever
    {
        private static DcrGraph _inputGraph;
        private static Log _inputLog;

        // Data
        //...

        public static QualityDimensions Retrieve(DcrGraph inputGraph, Log inputLog)
        {
            _inputGraph = inputGraph;
            _inputLog = inputLog;


            var result = new QualityDimensions
            {
                Fitness = GetFitness(),
                Simplicity = GetSimplicity(),
                Precision = GetPrecision(null)
            };
            return result;
        }

        public static QualityDimensions Retrieve(DcrGraph inputGraph, Log inputLog,
            Dictionary<byte[], int> uniqueStatesWithRunnableActivityCount)
        {
            _inputGraph = inputGraph;
            _inputLog = inputLog;


            var result = new QualityDimensions
            {
                Fitness = GetFitness(),
                Simplicity = GetSimplicity(),
                Precision = GetPrecision(uniqueStatesWithRunnableActivityCount)
            };
            return result;
        }

        /// <summary>
        /// Divides the amount of traces replayable by the _inputGraph with the total amount of traces in the _inputLog, multiplied by 100.
        /// </summary>
        /// <returns>The fitness percentage of the _inputGraph with respects to the _inputLog.</returns>
        private static double GetFitness()
        {
            if (_inputLog.Traces.Count == 0) return 100.0; // TODO: maybe actually 0 % ?

            var tracesReplayed = 0.0;
            foreach (var logTrace in _inputLog.Traces)
            {
                var graphCopy = _inputGraph.Copy();
                graphCopy.Running = true;
                var success = true;
                foreach (var logEvent in logTrace.Events)
                {
                    try
                    {
                        if (!graphCopy.Execute(graphCopy.GetActivity(logEvent.IdOfActivity)))
                        {
                            success = false;
                            break;
                        }
                    }
                    catch (ArgumentNullException)
                    {
                        success = false;
                        break;
                    }
                }
                if (success && graphCopy.IsFinalState())
                {
                    // All executions succeeded
                    tracesReplayed++;
                }
            }

            var tracesInLog = _inputLog.Traces.Count;

            return (tracesReplayed / tracesInLog) * 100.0;
        }

        /// <summary>
        /// Divides the amount of relations in the _inputGraph with the total amount of relations that could have been in the graph.
        /// Then divides the amount of relation couples (For instance relation between A and B, regardless of direction) with the
        /// total possible amount of relation couples.
        /// Then divides both of the above by two and adds them together, so that both calculations have an equal say in the
        /// resulting simplicity.
        /// In the end multiplies by 100 for percentage representation.
        /// </summary>
        /// <returns>The simplicity percentage of the _inputGraph.</returns>
        private static double GetSimplicity()
        {
            //TODO: account for (start-states) pending and excluded when measuring

            var relationsInGraph = _inputGraph.Conditions.Values.Sum(x => x.Count) + _inputGraph.IncludeExcludes.Values.Sum(x => x.Count) +
                _inputGraph.Responses.Values.Sum(x => x.Count) + _inputGraph.Milestones.Values.Sum(x => x.Count);
            var possibleRelations = _inputGraph.Activities.Count * _inputGraph.Activities.Count * 4.0 - _inputGraph.Activities.Count * 3.0; // TODO: Correct?

            var possibleRelationCouples = Math.Pow(_inputGraph.Activities.Count, 2.0);
            var relationCouples = new HashSet<RelationCouple>();
            GatherRelationCouples(_inputGraph.Conditions, relationCouples);
            GatherRelationCouples(_inputGraph.Responses, relationCouples);
            GatherRelationCouples(_inputGraph.Milestones, relationCouples);
            GatherRelationCouples(DcrGraph.ConvertToDictionaryActivityHashSetActivity(_inputGraph.IncludeExcludes), relationCouples);

            var totalRelationsPart = (1.0 - relationsInGraph / possibleRelations) / 2.0;
            var relationCouplesPart = (1.0 - relationCouples.Count / possibleRelationCouples) / 2.0;

            return (totalRelationsPart + relationCouplesPart) * 100.0;
        }

        private static void GatherRelationCouples(Dictionary<Activity, HashSet<Activity>> dictionary, HashSet<RelationCouple> relationCouples)
        {
            foreach (var relation in dictionary)
            {
                foreach (var target in relation.Value)
                {
                    relationCouples.Add(new RelationCouple(relation.Key, target));
                }
            }
        }
        
        private static double GetPrecision(Dictionary<byte[], int> uniqueStatesWithRunnableActivityCount)
        {
            if (uniqueStatesWithRunnableActivityCount == null)
            {
                uniqueStatesWithRunnableActivityCount = UniqueStateFinder.GetUniqueStatesWithRunnableActivityCount(_inputGraph);
            }
            
            var legalActivitiesExecutedInStates = uniqueStatesWithRunnableActivityCount.ToDictionary(state => state.Key, state => new HashSet<string>(), new ByteArrayComparer());
            var illegalActivitiesExecutedInStates = uniqueStatesWithRunnableActivityCount.ToDictionary(state => state.Key, state => new HashSet<string>(), new ByteArrayComparer());

            foreach (var logTrace in _inputLog.Traces)
            {
                var currentGraph = _inputGraph.Copy();
                currentGraph.Running = true;
                foreach (var logEvent in logTrace.Events)
                {
                    try
                    {
                        if (currentGraph.Execute(currentGraph.GetActivity(logEvent.IdOfActivity)))
                        {
                            legalActivitiesExecutedInStates[DcrGraph.HashDcrGraph(currentGraph)].Add(logEvent.IdOfActivity);
                        }
                        else
                        {
                            illegalActivitiesExecutedInStates[DcrGraph.HashDcrGraph(currentGraph)].Add(logEvent.IdOfActivity);
                        }
                    }
                    catch (ArgumentNullException)
                    {
                        illegalActivitiesExecutedInStates[DcrGraph.HashDcrGraph(currentGraph)].Add(logEvent.IdOfActivity);
                    }
                }
            }

            // Sum up resulting values
            var legalActivitiesThatCanBeExecuted = uniqueStatesWithRunnableActivityCount.Values.Sum();
            var legalActivitiesExecuted = legalActivitiesExecutedInStates.Sum(x => x.Value.Count);
            var illegalActivitiesExecuted = illegalActivitiesExecutedInStates.Sum(x => x.Value.Count);
            
            if (legalActivitiesThatCanBeExecuted + illegalActivitiesExecuted == 0)
            {
                return 0.0; // Avoid division by zero
            }
            return ((double) legalActivitiesExecuted / (legalActivitiesThatCanBeExecuted + illegalActivitiesExecuted)) * 100.0;
        }
        
    }
}
