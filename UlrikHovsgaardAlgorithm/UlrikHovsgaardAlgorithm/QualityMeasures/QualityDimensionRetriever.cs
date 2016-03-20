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
                Fitness = GetFitnessSimple(),
                Simplicity = GetSimplicitySimple(),
                Precision = GetPrecisionComplicated(),
                Generalization = GetGeneralizationAcitivityBased()
            };
            return result;
        }

        /// <summary>
        /// Divides the amount of traces replayable by the _inputGraph with the total amount of traces in the _inputLog, multiplied by 100.
        /// </summary>
        /// <returns>The fitness percentage of the _inputGraph with respects to the _inputLog.</returns>
        private static double GetFitnessSimple()
        {
            var tracesReplayed = 0.0;
            foreach (var logTrace in _inputLog.Traces)
            {
                var graphCopy = _inputGraph.Copy();
                graphCopy.Running = true;
                var success = true;
                foreach (var logEvent in logTrace.Events)
                {
                    if (!graphCopy.Execute(graphCopy.GetActivity(logEvent.IdOfActivity)))
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
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
        private static double GetSimplicitySimple()
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
            GatherRelationCouples(DcrGraph.ConvertToDictionaryActivityHashSetActivity(_inputGraph.Deadlines), relationCouples);

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

        //static Dictionary<Activity, HashSet<Activity>> Merge<TKey, TValue>(this IEnumerable<Dictionary<Activity, HashSet<Activity>>> enumerable)
        //{
        //    // Doesn't work... Smashed @ duplicate keys
        //    return enumerable.SelectMany(x => x).ToDictionary(x => x.Key, y => y.Value);
        //}

        /// <summary>
        /// Divides the amount of unique traces in the _inputLog with the total amount of unique traces allowed in the _inputGraph, multiplied by 100.
        /// </summary>
        /// <returns>The precision percentage of the _inputGraph with respects to the _inputLog.</returns>
        // 
        private static decimal GetPrecisionSimple()
        {
            var graphUniqueTraces = new UniqueTraceFinderWithComparison(_inputGraph).TracesToBeComparedTo;

            var uniqueTracesInLog = new List<string>();
            foreach (var logTrace in _inputLog.Traces)
            {
                var logAsString = logTrace.ToStringForm();
                if (!uniqueTracesInLog.Contains(logAsString))
                {
                    // If not allowed by graph, doesn't count
                    if (graphUniqueTraces.Any(graphUniqueTrace => graphUniqueTrace.ToStringForm() == logAsString))
                    {
                        uniqueTracesInLog.Add(logAsString);
                    }
                }
            }

            if (graphUniqueTraces.Count == 0)
            {
                return decimal.MinusOne;
            }
            return decimal.Multiply(decimal.Divide(uniqueTracesInLog.Count, graphUniqueTraces.Count), new decimal(100));
        }

        private static double GetPrecisionComplicated()
        {
            var allStatesInGraph = UniqueStateFinder.GetUniqueStates(_inputGraph);

            var activitiesExecutableInStates = allStatesInGraph.ToDictionary(DcrGraph.HashDcrGraph, state => 0.0, new ByteArrayComparer());
            var legalActivitiesExecutedInStates = allStatesInGraph.ToDictionary(DcrGraph.HashDcrGraph, state => new HashSet<string>(), new ByteArrayComparer());
            var illegalActivitiesExecutedInStates = allStatesInGraph.ToDictionary(DcrGraph.HashDcrGraph, state => new HashSet<string>(), new ByteArrayComparer());

            foreach (var logTrace in _inputLog.Traces)
            {
                var currentGraph = _inputGraph.Copy();
                currentGraph.Running = true;
                foreach (var logEvent in logTrace.Events)
                {
                    // Store 
                    activitiesExecutableInStates[DcrGraph.HashDcrGraph(currentGraph)] = currentGraph.GetRunnableActivities().Count;

                    if (currentGraph.Execute(currentGraph.GetActivity(logEvent.IdOfActivity)))
                    {
                        legalActivitiesExecutedInStates[DcrGraph.HashDcrGraph(currentGraph)].Add(logEvent.IdOfActivity);
                    }
                    else
                    {
                        illegalActivitiesExecutedInStates[DcrGraph.HashDcrGraph(currentGraph)].Add(logEvent.IdOfActivity);
                    }
                }
            }

            // Sum up resulting values
            var legalActivitiesThatCanBeExecuted = activitiesExecutableInStates.Values.Sum();
            var legalActivitiesExecuted = legalActivitiesExecutedInStates.Sum(x => x.Value.Count);
            var illegalActivitiesExecuted = illegalActivitiesExecutedInStates.Sum(x => x.Value.Count);
            
            if ((legalActivitiesThatCanBeExecuted + illegalActivitiesExecuted).Equals(0.0))
            {
                return 0.0; // Avoid division by zero
            }
            return (legalActivitiesExecuted / (legalActivitiesThatCanBeExecuted + illegalActivitiesExecuted)) * 100.0;
        }

        /// <summary>
        /// Divides the summed frequencies with which each Activity must be visited to replay the log with the amount of activities in _inputGraph, multiplied by 100:
        /// TODO: Health-check along the way? Ignore not-replayable traces, or...?
        /// TODO: Maybe only consider "legal" executions! (Right now, a graph with all activities excluded gets some kind of generalization...)
        /// </summary>
        /// <returns>The generalization percentage of the _inputGraph with respects to the _inputLog.</returns>
        private static double GetGeneralizationAcitivityBased()
        {
            // Dictionary<ActivityID, #executions>
            var activityExecutionCounts = new Dictionary<string, int>();
            foreach (var logTrace in _inputLog.Traces)
            {
                var graphCopy = _inputGraph.Copy();
                graphCopy.Running = true;
                foreach (var logEvent in logTrace.Events)
                {
                    if (graphCopy.Execute(graphCopy.GetActivity(logEvent.IdOfActivity)))
                    {
                        int count;
                        if (activityExecutionCounts.TryGetValue(logEvent.IdOfActivity, out count))
                        {
                            activityExecutionCounts[logEvent.IdOfActivity] = ++count;
                        }
                        else
                        {
                            activityExecutionCounts.Add(logEvent.IdOfActivity, 1);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
            // If this value becomes equal to the amount of activities, then Generalization = 0... Intended? TODO think
            double sumOfNodeExecutionsSqrt = activityExecutionCounts.Values.Sum(count => Math.Sqrt(count));
            
            return (1.0 - (Math.Pow(sumOfNodeExecutionsSqrt, -1) / _inputGraph.Activities.Count)) * 100.0;
        }

        // Bad way to go about it, actually... Unless proper formula can be figured out
        // Basic idea: How many times was each unique trace executed compared to the total amount of unique traces
        private static decimal GetGeneralizationTraceBased()
        {
            //// Dictionary<TraceAsString, #executions>
            //var activityExecutionCounts = new Dictionary<string, int>();
            //foreach (var logTrace in _inputLog.Traces)
            //{
            //    int count;
            //    if (activityExecutionCounts.TryGetValue(logTrace.ToStringForm(), out count))
            //    {
            //        activityExecutionCounts[logEvent.IdOfActivity] = ++count;
            //    }
            //    else
            //    {
            //        activityExecutionCounts.Add(logEvent.IdOfActivity, 1);
            //    }
            //}
            //decimal sumOfNodeExecutionsSqrt = activityExecutionCounts.Values.Sum(count => (decimal)Math.Pow(Math.Sqrt(count), -1));

            //// (1 - (sumOfNodeExecutionsSqrt / #nodesInTree)) * 100
            //return decimal.Multiply(decimal.Subtract(decimal.One, decimal.Divide(sumOfNodeExecutionsSqrt, _inputGraph.Activities.Count)), new decimal(100));
            return new decimal(1);
        }
    }
}
