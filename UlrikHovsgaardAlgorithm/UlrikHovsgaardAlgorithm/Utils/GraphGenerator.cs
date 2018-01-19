using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;

namespace UlrikHovsgaardAlgorithm.Utils
{
    public class GraphGenerator
    {
        public static IEnumerable<DcrGraphSimple> Generate(int alphabetSize, int relationsCap, int numberOfGraphs, bool doSelfConditions)
        {
            var names = new List<string>();
            for (int i = 0; i < alphabetSize; i++)
                names.Add("" + i);

            var rand = new Random(0);
            var all = Enumerable.Range(0, numberOfGraphs).Select(x => GenerateRandomActivities(rand, names)).ToList();
            return all.Select(x => GenerateGraph(rand, x, relationsCap, doSelfConditions));
        }

        public static IEnumerable<DcrGraphSimple> Generate(int alphabetSize, int relationsCap, int numberOfGraphs, Func<DcrGraphSimple, bool> validator, bool doSelfConditions)
        {
            var names = new List<string>();
            for (int i = 0; i < alphabetSize; i++)
                names.Add("a" + i);

            var rand = new Random(0);

            var graphs = new List<DcrGraphSimple>();
            while (graphs.Count < numberOfGraphs)
            {
                Console.WriteLine($"Graph count = {graphs.Count}");
                var activities = GenerateRandomActivities(rand, names);
                var graph = GenerateGraph(rand, activities, relationsCap, doSelfConditions);
                Console.WriteLine("Graph generated");
                if (graph.RelationsCount != relationsCap)
                    Console.WriteLine("Count is off");

                if (validator(graph))
                {
                    Console.WriteLine("Validation Succeeded!");
                    graphs.Add(graph);
                }
            }
            
            return graphs;
        }

        public static DcrGraphSimple GenerateGraph(Random rand, List<Activity> activities, int relationsCap, bool doSelfConditions)
        {
            var graph = new DcrGraphSimple(new HashSet<Activity>(activities));

            var relationsCount = 0;
            
            var numberOfActivities = activities.Count;
            while (relationsCount < relationsCap)
            {
                var sourceInt = rand.Next(numberOfActivities);
                var source = activities[sourceInt];
                var targetInt = rand.Next(numberOfActivities);
                var target = activities[targetInt];

                var relationType = rand.Next(4);
                
                //Console.ReadLine();
                if (relationType == 0 && !(source.HasIncludeTo(target, graph) || source.HasExcludeTo(target, graph))
                    && !source.Equals(target)) // include (no self-inclusions generated)
                {
                    graph.AddInclude(source, target);
                    relationsCount++;
                }
                else if (relationType == 1 && !(source.HasExcludeTo(target, graph) || source.HasIncludeTo(target, graph))) //exclude
                {
                    graph.AddExclude(source, target);
                    relationsCount++;
                }
                else if (relationType == 2 && !source.HasResponseTo(target, graph) && sourceInt != targetInt) // response
                {
                    graph.AddResponse(source, target);
                    relationsCount++;
                }
                else if (relationType == 3 && !source.HasConditionTo(target, graph)) // condition
                {
                    if (!source.Equals(target) || (doSelfConditions && rand.Next(8) == 0)) // Also applying another 12.5 % chance to do a self-condition
                    {
                        graph.AddCondition(source, target);
                        relationsCount++;
                    }
                }
            }

            // Post-processing:
            MakeAllActivitiesIncludable(rand, graph);

            return graph;
        }

        public static void MakeAllActivitiesIncludable(Random rand, DcrGraphSimple graph)
        {
            // To make sure we have a "meaningful" graph of the given size, ensure that all activities are (statically) includable
            while (graph.IncludesInverted.Keys.Count < graph.Activities.Count)
            {
                foreach (var act in graph.Activities)
                {
                    // If no include targets this activity
                    if (!graph.IncludesInverted.ContainsKey(act))
                    {
                        // Look for an exclude to invert
                        if (graph.ExcludesInverted.TryGetValue(act, out var excludeSources))
                        {
                            var excludeToInvertIndex = rand.Next(excludeSources.Count);
                            var excludeToInvert = excludeSources.ToList()[excludeToInvertIndex];

                            // Replacing a relation ensures that the relation-count stays the same
                            graph.RemoveExclude(excludeToInvert, act);
                            graph.AddInclude(excludeToInvert, act);
                        }
                        else
                        {
                            // Remove some arbitrary constraining relation (to ensure that relation-count still matches the given parameter)
                            // TODO: Currently MAY not manage to remove such a relation (giving amount of retries)
                            RemoveRandomConstrainingRelation(rand, graph, retriesLeft: 5);

                            // Get activities other than "act":
                            var otherActivities = graph.Activities.Where(a => !a.Equals(act)).ToList();

                            // Add include from random source
                            var includeSourceIndex = rand.Next(otherActivities.Count);
                            var includeSource = otherActivities.ToList()[includeSourceIndex];

                            graph.AddInclude(includeSource, act);
                        }
                    }
                }
            }
        }
        
        private static void RemoveRandomConstrainingRelation(Random rand, DcrGraphSimple graph, int retriesLeft)
        {
            if (retriesLeft == 0) return;

            var relationType = rand.Next(3);

            if (relationType == 0) // exclude
            {
                var (source, target) = GetRandomSourceAndTarget(rand, graph.Excludes);
                if (source == null || target == null)
                {
                    RemoveRandomConstrainingRelation(rand, graph, retriesLeft - 1); // Retry if couldn't find source and target
                    return;
                }

                graph.RemoveExclude(source, target);
            }
            else if (relationType == 1) // response
            {
                var (source, target) = GetRandomSourceAndTarget(rand, graph.Responses);
                if (source == null || target == null)
                {
                    RemoveRandomConstrainingRelation(rand, graph, retriesLeft - 1); // Retry if couldn't find source and target
                    return;
                }

                graph.RemoveResponse(source, target);
            }
            else if (relationType == 2) // condition
            {
                var (source, target) = GetRandomSourceAndTarget(rand, graph.Conditions);
                if (source == null || target == null)
                {
                    RemoveRandomConstrainingRelation(rand, graph, retriesLeft - 1); // Retry if couldn't find source and target
                    return;
                }

                graph.RemoveCondition(source, target);
            }
        }

        private static (Activity, Activity) GetRandomSourceAndTarget(Random rand, Dictionary<Activity, HashSet<Activity>> relationDictionary)
        {
            if (relationDictionary.Keys.Count == 0) // Ensure there is some source
                return (null, null);

            var sourceIndex = rand.Next(relationDictionary.Keys.Count);
            var source = relationDictionary.ToList()[sourceIndex].Key;

            if (relationDictionary[source].Count == 0) // Ensure there is some target
                return (null, null);

            var targetIndex = rand.Next(relationDictionary[source].Count);
            var target = relationDictionary[source].ToList()[targetIndex];

            return (source, target);
        }

        public static List<Activity> GenerateRandomActivities(Random rand, List<string> names)
        {
            return names.Select(name => Tuple2Activity(name, (rand.Next(2) == 0, false, rand.Next(2) == 0))).ToList();
        }

        public static Activity Tuple2Activity(string name, (bool, bool, bool) tuple)
        {
            var act = new Activity(name, name);
            act.Included = tuple.Item1;
            act.Executed = tuple.Item2;
            act.Pending = tuple.Item3;
            return act;
        }
    }
}
