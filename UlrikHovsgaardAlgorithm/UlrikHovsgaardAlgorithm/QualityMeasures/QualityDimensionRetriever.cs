﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using UlrikHovsgaardAlgorithm.Utils;

namespace UlrikHovsgaardAlgorithm.QualityMeasures
{
    public static class QualityDimensionRetriever
    {
        private const bool _newPrecision = true;

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
                Precision = _newPrecision ? GetPrecisionNew() : GetPrecision(null),
                Generality = GetGenerality()
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
                Precision = _newPrecision ? GetPrecisionNew() : GetPrecision(uniqueStatesWithRunnableActivityCount),
                Generality = GetGenerality()
                
            };
            return result;
        }

        /// <summary>
        /// Divides the amount of traces replayable by the _inputGraph with the total amount of traces in the _inputLog, multiplied by 100.
        /// </summary>
        /// <returns>The fitness percentage of the _inputGraph with respects to the _inputLog.</returns>
        private static double GetFitness()
        {
            if (_inputLog.Traces.Count == 0) return 100.0; 

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
            double numberOfActivities = (double) _inputGraph.GetActivities().Count; //does not include potential nested graphs
            double pendingActivities = (double) _inputGraph.GetActivities().Count(a => a.Pending);
            double allActivities = numberOfActivities +
                                    _inputGraph.Activities.Count(a => a.IsNestedGraph); 

            double relationsInGraph = _inputGraph.Conditions.Values.Sum(x => x.Count) +
                                   _inputGraph.IncludeExcludes.Values.Sum(x => x.Count) +
                                   _inputGraph.Responses.Values.Sum(x => x.Count) +
                                   _inputGraph.Milestones.Values.Sum(x => x.Count);
            //Also count relations in the possible nested graphs
            //+ _inputGraph.Activities.Where(a => a.IsNestedGraph).Select(b => b.NestedGraph).Sum(nestedGraph => nestedGraph.Conditions.Values.Sum(x => x.Count) + nestedGraph.IncludeExcludes.Values.Sum(x => x.Count) + nestedGraph.Responses.Values.Sum(x => x.Count) + nestedGraph.Milestones.Values.Sum(x => x.Count));
            double possibleRelations = (allActivities * allActivities * 4.0 - allActivities * 3.0);

            // Possible relation couples = n + n*(n-1) / 2
            double possibleRelationCouples = allActivities + (allActivities * (allActivities - 1) / 2);
            var relationCouples = new HashSet<RelationCouple>();
            GatherRelationCouples(_inputGraph.Conditions, relationCouples);
            GatherRelationCouples(_inputGraph.Responses, relationCouples);
            GatherRelationCouples(_inputGraph.Milestones, relationCouples);
            GatherRelationCouples(DcrGraph.ConvertToDictionaryActivityHashSetActivity(_inputGraph.IncludeExcludes), relationCouples);
            foreach (var nestedGraph in _inputGraph.Activities.Where(a => a.IsNestedGraph).Select(b => b.NestedGraph)) 
            {
                GatherRelationCouples(nestedGraph.Conditions, relationCouples);
                GatherRelationCouples(nestedGraph.Responses, relationCouples);
                GatherRelationCouples(nestedGraph.Milestones, relationCouples);
                GatherRelationCouples(DcrGraph.ConvertToDictionaryActivityHashSetActivity(nestedGraph.IncludeExcludes), relationCouples);
            }
            

            double totalRelationsPart = (1.0 - relationsInGraph / possibleRelations) / 2; // 50 % weight
            double relationCouplesPart = (1.0 - relationCouples.Count / possibleRelationCouples) / 2; // 50 % weight
            double pendingPart = (pendingActivities/numberOfActivities) * 0.1; // up to 10 % negative weight
            double nestedGraphPart = (GetNestedGraphCount(_inputGraph) / numberOfActivities) * 0.2; // 20 % negative weight when #nestedGraphs == #activities - could be even less

            var result = (totalRelationsPart + relationCouplesPart - pendingPart - nestedGraphPart) * 100.0;

            return result > 0.0 ? result : 0.0;
        }

        private static int GetNestedGraphCount(DcrGraph graph)
        {
            var result = 0;
            foreach (var nestedGraph in graph.Activities.Where(a => a.IsNestedGraph).Select(b => b.NestedGraph))
            {
                result++;
                result += GetNestedGraphCount(nestedGraph);
            }
            return result;
        }

        private static void GatherRelationCouples(Dictionary<Activity, HashSet<Activity>> dictionary, HashSet<RelationCouple> relationCouples)
        {
            foreach (var relation in dictionary)
            {
                foreach (var target in relation.Value)
                {
                    if (!relationCouples.Add(new RelationCouple(relation.Key, target)))
                    {
                        var i = 0;
                        i++;
                    }
                }
            }
        }

        private static double GetGenerality()
        {
            //gives us a mapping of id to amount of times it is mentioned in the log.
            List<int> executions =  _inputGraph.Activities.Select(a => _inputLog.Traces.SelectMany(t => t.Events.Where(e2 => e2.IdOfActivity == a.Id)).Count()).ToList();

            double generalizationSum = executions.Sum(count => 1/Math.Sqrt(count));

            return (1 - (generalizationSum / executions.Count())) * 100;
        }
        
        private static double GetPrecision(Dictionary<byte[], int> uniqueStatesWithRunnableActivityCount)
        {
            if (uniqueStatesWithRunnableActivityCount == null)
            {
                uniqueStatesWithRunnableActivityCount = UniqueStateFinder.GetUniqueStatesWithRunnableActivityCount(_inputGraph);
                var cnt = UniqueStateFinder.Counter;
            }
            
            var legalActivitiesExecutedInStates = uniqueStatesWithRunnableActivityCount.ToDictionary(state => state.Key, state => new HashSet<string>(), new ByteArrayComparer());
            var illegalActivitiesExecutedInStates = uniqueStatesWithRunnableActivityCount.ToDictionary(state => state.Key, state => new HashSet<string>(), new ByteArrayComparer());

            foreach (var logTrace in _inputLog.Traces)
            {
                var currentGraph = _inputGraph.Copy();
                currentGraph.Running = true;
                foreach (var logEvent in logTrace.Events)
                {
                    var copy = currentGraph.Copy();
                    try
                    {
                        if (currentGraph.Execute(currentGraph.GetActivity(logEvent.IdOfActivity)))
                        {
                            legalActivitiesExecutedInStates[DcrGraph.HashDcrGraph(copy)].Add(logEvent.IdOfActivity);
                        }
                        else
                        {
                            illegalActivitiesExecutedInStates[DcrGraph.HashDcrGraph(copy)].Add(logEvent.IdOfActivity);
                        }
                    }
                    catch (ArgumentNullException)
                    {
                        illegalActivitiesExecutedInStates[DcrGraph.HashDcrGraph(copy)].Add(logEvent.IdOfActivity);
                    }
                }
            }

            // Sum up resulting values
            var legalActivitiesThatCanBeExecuted = uniqueStatesWithRunnableActivityCount.Values.Sum();
            var legalActivitiesExecuted = legalActivitiesExecutedInStates.Sum(x => x.Value.Count);
            var illegalActivitiesExecuted = illegalActivitiesExecutedInStates.Sum(x => x.Value.Count);
            
            if (legalActivitiesThatCanBeExecuted + illegalActivitiesExecuted == 0)
            {
                //this means that we don't allow any activities to be executed ('everything is illegal' or empty graph)
                //and that we don't execute anything (empty log)
                //we also avoid division by 0
                return 100.0;
            }
            var d1 = Math.Log(legalActivitiesExecuted);
            var d2 = Math.Log(legalActivitiesThatCanBeExecuted+ illegalActivitiesExecuted);

            return (d1/d2)*100.0;

            //return ((double) legalActivitiesExecuted / (legalActivitiesThatCanBeExecuted + illegalActivitiesExecuted)) * 100.0;
        }


        private static double GetPrecisionNew()
        {
            var seenStatesWithRunnableActivityCount = new Dictionary<byte[], int>(new ByteArrayComparer());
            var legalActivitiesExecutedInStates     = new Dictionary<byte[], HashSet<string>>(new ByteArrayComparer());

            // Expand discovered state-space (Here assuming _inputGraph is in its unmodified start-state)
            StoreRunnableActivityCount(seenStatesWithRunnableActivityCount, DcrGraph.HashDcrGraph(_inputGraph), _inputGraph.GetRunnableActivities().Count);

            foreach (var logTrace in _inputLog.Traces)
            {
                var currentGraph = _inputGraph.Copy();
                currentGraph.Running = true;

                foreach (var logEvent in logTrace.Events)
                {
                    try
                    {
                        var hashedGraphBeforeExecution = DcrGraph.HashDcrGraph(currentGraph);
                        if (currentGraph.Execute(currentGraph.GetActivity(logEvent.IdOfActivity)))
                        {
                            var hashedGraphAfterExecution = DcrGraph.HashDcrGraph(currentGraph);
                            // Store successful choice (execution) of path (option)
                            StoreSuccessfulPathChoice(legalActivitiesExecutedInStates, hashedGraphBeforeExecution, logEvent.IdOfActivity);
                            // Expand discovered state-space
                            StoreRunnableActivityCount(seenStatesWithRunnableActivityCount, hashedGraphAfterExecution, currentGraph.GetRunnableActivities().Count);
                        }
                    }
                    catch (ArgumentNullException)
                    {
                        // No such activity exists
                    }
                }
            }

            // Sum up resulting values
            var legalActivitiesThatCouldHaveBeExecuted = seenStatesWithRunnableActivityCount.Values.Sum();
            var legalActivitiesExecuted = legalActivitiesExecutedInStates.Sum(x => x.Value.Count);

            if (legalActivitiesThatCouldHaveBeExecuted == 0)
            {
                //this means that we don't allow any activities to be executed ('everything is illegal' or empty graph)
                //and that we don't execute anything (empty log)
                //we also avoid division by 0
                return 100.0;
            }
            double d1 = legalActivitiesExecuted;
            double d2 = legalActivitiesThatCouldHaveBeExecuted;

            var res = (d1 / d2) * 100.0;
            return res;

            //return ((double) legalActivitiesExecuted / (legalActivitiesThatCanBeExecuted + illegalActivitiesExecuted)) * 100.0;
        }

        private static void StoreSuccessfulPathChoice(Dictionary<byte[], HashSet<string>> byteArrToStrings, byte[] byteArr, string val)
        {
            if (byteArrToStrings.ContainsKey(byteArr))
            {
                // Add given value
                byteArrToStrings[byteArr].Add(val);
            }
            else
            {
                // Initialize set of strings with given value
                byteArrToStrings[byteArr] = new HashSet<string> { val };
            }
        }

        private static void StoreRunnableActivityCount(Dictionary<byte[], int> byteArrToInt, byte[] byteArr, int val)
        {
            if (!byteArrToInt.ContainsKey(byteArr))
            {
                byteArrToInt[byteArr] = val;
            }
            // Otherwise do nothing - the value has been stored previously
        }
    }
}
