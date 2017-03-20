using System;
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
        private const bool _newMeasures = true;

        public static QualityDimensions Retrieve(DcrGraph graph, Log log)
        {
            var result = new QualityDimensions
            {
                Fitness = GetFitness(log, graph),
                Simplicity = _newMeasures ? GetSimplicityNew(graph) : GetSimplicity(graph),
                Precision = _newMeasures ? GetPrecisionNew(log, graph) : GetPrecision(log, graph, null),
                Generality = GetGenerality(log, graph)
            };
            return result;
        }

        public static QualityDimensions Retrieve(DcrGraph graph, Log log,
            Dictionary<byte[], int> uniqueStatesWithRunnableActivityCount)
        {
            var result = new QualityDimensions
            {
                Fitness = GetFitness(log, graph),
                Simplicity = _newMeasures ? GetSimplicityNew(graph) : GetSimplicity(graph),
                Precision = _newMeasures ? GetPrecisionNew(log, graph) : GetPrecision(log, graph, uniqueStatesWithRunnableActivityCount),
                Generality = GetGenerality(log, graph)
            };
            return result;
        }

        /// <summary>
        /// Divides the amount of traces replayable by the _inputGraph with the total amount of traces in the _inputLog, multiplied by 100.
        /// </summary>
        /// <returns>The fitness percentage of the _inputGraph with respects to the _inputLog.</returns>
        public static double GetFitness(Log log, DcrGraph graph)
        {
            if (log.Traces.Count == 0) return 100.0; 

            var tracesReplayed = 0.0;
            foreach (var logTrace in log.Traces)
            {
                var graphCopy = graph.Copy();
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

            var tracesInLog = log.Traces.Count;

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
        public static double GetSimplicity(DcrGraph graph)
        {
            double numberOfActivities = (double)graph.GetActivities().Count; //does not include potential nested graphs
            double pendingActivities = (double)graph.GetActivities().Count(a => a.Pending);
            double allActivities = numberOfActivities +
                                    graph.Activities.Count(a => a.IsNestedGraph); 

            double relationsInGraph = graph.Conditions.Values.Sum(x => x.Count) +
                                   graph.IncludeExcludes.Values.Sum(x => x.Count) +
                                   graph.Responses.Values.Sum(x => x.Count) +
                                   graph.Milestones.Values.Sum(x => x.Count);
            //Also count relations in the possible nested graphs
            //+ _inputGraph.Activities.Where(a => a.IsNestedGraph).Select(b => b.NestedGraph).Sum(nestedGraph => nestedGraph.Conditions.Values.Sum(x => x.Count) + nestedGraph.IncludeExcludes.Values.Sum(x => x.Count) + nestedGraph.Responses.Values.Sum(x => x.Count) + nestedGraph.Milestones.Values.Sum(x => x.Count));
            double possibleRelations = (allActivities * allActivities * 4.0 - allActivities * 3.0);

            // Possible relation couples = n + n*(n-1) / 2
            double possibleRelationCouples = allActivities + (allActivities * (allActivities - 1) / 2);
            var relationCouples = new HashSet<RelationCouple>();
            GatherRelationCouples(graph.Conditions, relationCouples);
            GatherRelationCouples(graph.Responses, relationCouples);
            GatherRelationCouples(graph.Milestones, relationCouples);
            GatherRelationCouples(graph.IncludeExcludes, relationCouples);
            foreach (var nestedGraph in graph.Activities.Where(a => a.IsNestedGraph).Select(b => b.NestedGraph)) 
            {
                GatherRelationCouples(nestedGraph.Conditions, relationCouples);
                GatherRelationCouples(nestedGraph.Responses, relationCouples);
                GatherRelationCouples(nestedGraph.Milestones, relationCouples);
                GatherRelationCouples(nestedGraph.IncludeExcludes, relationCouples);
            }
            

            double totalRelationsPart = (1.0 - relationsInGraph / possibleRelations) / 2; // 50 % weight
            double relationCouplesPart = (1.0 - relationCouples.Count / possibleRelationCouples) / 2; // 50 % weight
            double pendingPart = (pendingActivities/numberOfActivities) * 0.1; // up to 10 % negative weight
            double nestedGraphPart = (GetNestedGraphCount(graph) / numberOfActivities) * 0.2; // 20 % negative weight when #nestedGraphs == #activities - could be even less

            var result = (totalRelationsPart + relationCouplesPart - pendingPart - nestedGraphPart) * 100.0;

            return result > 0.0 ? result : 0.0;
        }

        public static double GetSimplicityNew(DcrGraph graph)
        {
            double numberOfActivities = (double)graph.GetActivities().Count; //does not include potential nested graphs
            double allActivities = numberOfActivities +
                                    graph.Activities.Count(a => a.IsNestedGraph);

            double relationsInGraph = graph.Conditions.Values.Sum(x => x.Count) +
                                   graph.IncludeExcludes.Values.Sum(x => x.Count) +
                                   graph.Responses.Values.Sum(x => x.Count) +
                                   graph.Milestones.Values.Sum(x => x.Count);

            //Also count relations in the possible nested graphs
            //+ _inputGraph.Activities.Where(a => a.IsNestedGraph).Select(b => b.NestedGraph).Sum(nestedGraph => nestedGraph.Conditions.Values.Sum(x => x.Count) + nestedGraph.IncludeExcludes.Values.Sum(x => x.Count) + nestedGraph.Responses.Values.Sum(x => x.Count) + nestedGraph.Milestones.Values.Sum(x => x.Count));
            double possibleRelations = (allActivities * allActivities * 4.0 - allActivities * 3.0);

            // Possible relation couples = n + n*(n-1) / 2
            double possibleRelationCouples = allActivities + (allActivities * (allActivities - 1) / 2);
            var relationCouples = new HashSet<RelationCouple>();
            GatherRelationCouples(graph.Conditions, relationCouples);
            GatherRelationCouples(graph.Responses, relationCouples);
            GatherRelationCouples(graph.Milestones, relationCouples);
            GatherRelationCouples(graph.IncludeExcludes, relationCouples);
            foreach (var nestedGraph in graph.Activities.Where(a => a.IsNestedGraph).Select(b => b.NestedGraph))
            {
                GatherRelationCouples(nestedGraph.Conditions, relationCouples);
                GatherRelationCouples(nestedGraph.Responses, relationCouples);
                GatherRelationCouples(nestedGraph.Milestones, relationCouples);
                GatherRelationCouples(nestedGraph.IncludeExcludes, relationCouples);
            }

            double totalRelationsPart = (1.0 - relationsInGraph / possibleRelations) / 2; // 50 % weight
            double relationCouplesPart = (1.0 - relationCouples.Count / possibleRelationCouples) / 2; // 50 % weight
            
            var result = (totalRelationsPart + relationCouplesPart) * 100.0;

            return result > 0.0 ? result : 0.0;
        }

        public static int GetNestedGraphCount(DcrGraph graph)
        {
            var result = 0;
            foreach (var nestedGraph in graph.Activities.Where(a => a.IsNestedGraph).Select(b => b.NestedGraph))
            {
                result++;
                result += GetNestedGraphCount(nestedGraph);
            }
            return result;
        }

        public static void GatherRelationCouples(Dictionary<Activity, Dictionary<Activity,Confidence>> dictionary, HashSet<RelationCouple> relationCouples)
        {
            foreach (var relation in dictionary)
            {
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(relation.Value))
                {
                    if (!relationCouples.Add(new RelationCouple(relation.Key, target)))
                    {
                        var i = 0;
                        i++;
                    }
                }
            }
        }

        public static double GetGenerality(Log log, DcrGraph graph)
        {
            //gives us a mapping of id to amount of times it is mentioned in the log.
            List<int> executions = graph.Activities.Select(a => log.Traces.SelectMany(t => t.Events.Where(e2 => e2.IdOfActivity == a.Id)).Count()).ToList();

            double generalizationSum = executions.Sum(count => 1/Math.Sqrt(count));

            return (1 - (generalizationSum / executions.Count())) * 100;
        }

        public static double GetPrecision(Log log, DcrGraph graph, Dictionary<byte[], int> uniqueStatesWithRunnableActivityCount)
        {
            if (uniqueStatesWithRunnableActivityCount == null)
            {
                uniqueStatesWithRunnableActivityCount = UniqueStateFinder.GetUniqueStatesWithRunnableActivityCount(graph);
                var cnt = UniqueStateFinder.Counter;
            }
            
            var legalActivitiesExecutedInStates = uniqueStatesWithRunnableActivityCount.ToDictionary(state => state.Key, state => new HashSet<string>(), new ByteArrayComparer());
            var illegalActivitiesExecutedInStates = uniqueStatesWithRunnableActivityCount.ToDictionary(state => state.Key, state => new HashSet<string>(), new ByteArrayComparer());

            foreach (var logTrace in log.Traces)
            {
                var currentGraph = graph.Copy();
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


        public static double GetPrecisionNew(Log log, DcrGraph graph)
        {
            var seenStatesWithRunnableActivityCount = new Dictionary<byte[], int>(new ByteArrayComparer());
            var legalActivitiesExecutedInStates     = new Dictionary<byte[], HashSet<string>>(new ByteArrayComparer());

            // Expand discovered state-space (Here assuming _inputGraph is in its unmodified start-state)
            StoreRunnableActivityCount(seenStatesWithRunnableActivityCount, DcrGraph.HashDcrGraph(graph), graph.GetRunnableActivities().Count);

            foreach (var logTrace in log.Traces)
            {
                var currentGraph = graph.Copy();
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
