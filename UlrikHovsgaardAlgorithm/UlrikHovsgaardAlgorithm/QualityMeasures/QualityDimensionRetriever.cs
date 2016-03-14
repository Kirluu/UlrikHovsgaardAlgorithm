using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithm.QualityMeasures
{
    public static class QualityDimensionRetriever
    {
        private static DcrGraph _inputGraph;
        private static Log _inputLog;

        // Data
        //...

        public static QualityDimensions RetrieveQualityDimensions(DcrGraph inputGraph, Log inputLog)
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
            foreach (var logTrace in _inputLog.Traces)
            {
                try
                {
                    var graphCopy = _inputGraph.Copy();
                    graphCopy.Running = true;
                    foreach (var logEvent in logTrace.Events)
                    {
                        graphCopy.Execute(graphCopy.GetActivity(logEvent.Id));
                    }
                    // All executions succeeded
                    tracesReplayed++;
                }
                catch (InvalidOperationException)
                {
                    // Do nothing, just continue to next trace
                }
            }

            var tracesInLog = _inputLog.Traces.Count;

            return decimal.Multiply(decimal.Divide(tracesReplayed, tracesInLog), new decimal(100));
        }

        /// <summary>
        /// Divides the amount of relations in the _inputGraph with the total amount of relations that could have been in the graph, multiplied by 100.
        /// </summary>
        /// <returns>The simplicity percentage of the _inputGraph.</returns>
        private static decimal GetSimplicitySimple()
        {
            var relationsInGraph = _inputGraph.Conditions.Values.Count + _inputGraph.IncludeExcludes.Values.Count + _inputGraph.Responses.Values.Count
                + _inputGraph.Milestones.Values.Count;
            var possibleRelations = _inputGraph.Activities.Count * _inputGraph.Activities.Count * 4 - _inputGraph.Activities.Count * 3; // TODO: Correct?

            return decimal.Multiply(decimal.Subtract(decimal.One, decimal.Divide(relationsInGraph, possibleRelations)), new decimal(100));
        }

        /// <summary>
        /// Divides the amount of unique traces in the _inputLog with the total amount of unique traces allowed in the _inputGraph, multiplied by 100.
        /// </summary>
        /// <returns>The precision percentage of the _inputGraph with respects to the _inputLog.</returns>
        private static decimal GetPrecisionSimple()
        {
            var uniqueTracesInLog = new List<string>();
            foreach (var logTrace in _inputLog.Traces)
            {
                var logAsString = logTrace.ToStringForm();
                if (!uniqueTracesInLog.Contains(logAsString))
                {
                    uniqueTracesInLog.Add(logAsString);
                }
            }

            var graphUniqueTraces = new UniqueTraceFinderWithComparison(_inputGraph).TracesToBeComparedTo;

            return decimal.Multiply(decimal.Divide(uniqueTracesInLog.Count, graphUniqueTraces.Count), new decimal(100));
        }

        /// <summary>
        /// Divides the summed frequencies with which each Activity must be visited to replay the log with the amount of activities in _inputGraph, multiplied by 100:
        /// TODO: Health-check along the way? Ignore not-replayable traces, or...?
        /// </summary>
        /// <returns>The generalization percentage of the _inputGraph with respects to the _inputLog.</returns>
        private static decimal GetGeneralizationAcitivityBased()
        {
            // Dictionary<ActivityID, #executions>
            var activityExecutionCounts = new Dictionary<string, int>();
            foreach (var logTrace in _inputLog.Traces)
            {
                foreach (var logEvent in logTrace.Events)
                {
                    int count;
                    if (activityExecutionCounts.TryGetValue(logEvent.Id, out count))
                    {
                        activityExecutionCounts[logEvent.Id] = ++count;
                    }
                    else
                    {
                        activityExecutionCounts.Add(logEvent.Id, 1);
                    }
                }
            }
            decimal sumOfNodeExecutionsSqrt = activityExecutionCounts.Values.Sum(count => (decimal) Math.Pow(Math.Sqrt(count), -1));

            // (1 - (sumOfNodeExecutionsSqrt / #nodesInTree)) * 100
            return decimal.Multiply(decimal.Subtract(decimal.One, decimal.Divide(sumOfNodeExecutionsSqrt, _inputGraph.Activities.Count)), new decimal(100));
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
            //        activityExecutionCounts[logEvent.Id] = ++count;
            //    }
            //    else
            //    {
            //        activityExecutionCounts.Add(logEvent.Id, 1);
            //    }
            //}
            //decimal sumOfNodeExecutionsSqrt = activityExecutionCounts.Values.Sum(count => (decimal)Math.Pow(Math.Sqrt(count), -1));

            //// (1 - (sumOfNodeExecutionsSqrt / #nodesInTree)) * 100
            //return decimal.Multiply(decimal.Subtract(decimal.One, decimal.Divide(sumOfNodeExecutionsSqrt, _inputGraph.Activities.Count)), new decimal(100));
            return new decimal(1);
        }
    }
}
