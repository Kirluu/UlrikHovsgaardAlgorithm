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
        private static List<LogTrace> _inputLog;

        // Data
        //...

        public static QualityDimensions RetrieveQualityDimensions(DcrGraph inputGraph, List<LogTrace> inputLog)
        {
            _inputGraph = inputGraph;
            _inputLog = inputLog;


            var result = new QualityDimensions
            {
                Fitness = GetFitnessSimple(),
                Simplicity = GetSimplicitySimple(),
                Precision = GetPrecisionSimple(),
                Generalization = GetGeneralizationAcitivityBased()
            };
            return result;
        }

        /// <summary>
        /// Divides the amount of traces replayable by the _inputGraph with the total amount of traces in the _inputLog, multiplied by 100.
        /// </summary>
        /// <returns>The fitness percentage of the _inputGraph with respects to the _inputLog.</returns>
        private static decimal GetFitnessSimple()
        {
            var tracesReplayed = 0;
            foreach (var logTrace in _inputLog)
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

            var tracesInLog = _inputLog.Count;

            return decimal.Multiply(decimal.Divide(tracesReplayed, tracesInLog), new decimal(100));
        }

        /// <summary>
        /// Divides the amount of relations in the _inputGraph with the total amount of relations that could have been in the graph, multiplied by 100.
        /// </summary>
        /// <returns>The simplicity percentage of the _inputGraph.</returns>
        // TODO: Stronger/Stricter relationship between activity and relation amount?
        // TODO: Consider implementation of 100 % = Flower-graph, 0 % = All possible relations, 50 % = n^2 relations
        private static decimal GetSimplicitySimple()
        {
            //TODO: account for pending and excluded when measuring
            var relationsInGraph = _inputGraph.Conditions.Values.Sum(x => x.Count) + _inputGraph.IncludeExcludes.Values.Sum(x => x.Count) +
                _inputGraph.Responses.Values.Sum(x => x.Count) + _inputGraph.Milestones.Values.Sum(x => x.Count);
            var possibleRelations = _inputGraph.Activities.Count * _inputGraph.Activities.Count * 4 - _inputGraph.Activities.Count * 3; // TODO: Correct?

            var possibleRelationCouples = (decimal) Math.Pow(_inputGraph.Activities.Count, 2);
            var relationCouples = new HashSet<Tuple<Activity, Activity>>();


            var halfOfResult = decimal.Multiply(decimal.Subtract(decimal.One, decimal.Divide(relationsInGraph, possibleRelations)), new decimal(100));
            //var otherHalf = decimal.Multiply(decimal.Subtract(decimal.One, decimal.Divide()), new decimal(100));

            return halfOfResult;
        }

        /// <summary>
        /// Divides the amount of unique traces in the _inputLog with the total amount of unique traces allowed in the _inputGraph, multiplied by 100.
        /// </summary>
        /// <returns>The precision percentage of the _inputGraph with respects to the _inputLog.</returns>
        // 
        private static decimal GetPrecisionSimple()
        {
            var graphUniqueTraces = new UniqueTraceFinderWithComparison(_inputGraph).TracesToBeComparedTo;

            var uniqueTracesInLog = new List<string>();
            foreach (var logTrace in _inputLog)
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
            var legalActivitiesExecuted = 0;
            var legalActivitiesThatCanBeExecuted = 0;
            var illegalActivitiesExecuted = 0;

            var allStatesInGraph = UniqueStateFinder.GetUniqueStates(_inputGraph);

            var activitiesExecutedInStates = allStatesInGraph.ToDictionary(DcrGraph.HashDcrGraph, state => new HashSet<string>(), new ByteArrayComparer());
            // Dictionary to look up actual graph behind a state
            var stateToDcrGraph = allStatesInGraph.ToDictionary(DcrGraph.HashDcrGraph, state => state, new ByteArrayComparer());

            foreach (var logTrace in _inputLog)
            {
                var currentGraph = _inputGraph.Copy();
                currentGraph.Running = true;
                foreach (var logEvent in logTrace.Events)
                {
                    activitiesExecutedInStates[DcrGraph.HashDcrGraph(currentGraph)].Add(logEvent.IdOfActivity);

                    if (currentGraph.Execute(currentGraph.GetActivity(logEvent.IdOfActivity)))
                    {
                        
                    }

                }
            }

            return 0.0; // TODO
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
            foreach (var logTrace in _inputLog)
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
