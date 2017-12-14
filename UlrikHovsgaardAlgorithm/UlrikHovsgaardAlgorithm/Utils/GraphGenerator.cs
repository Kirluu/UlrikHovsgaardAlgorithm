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
                if (relationType == 0 && !(source.HasIncludeTo(target, graph) || source.HasExcludeTo(target, graph))) // include
                {
                    graph.AddInclude(source, target);
                    relationsCount++;
                } else if (relationType == 1 && !(source.HasExcludeTo(target, graph) || source.HasIncludeTo(target, graph))) //exclude
                {
                    graph.AddExclude(source, target);
                    relationsCount++;
                } else if (relationType == 2 && !source.HasResponseTo(target, graph) && sourceInt != targetInt) // response
                {
                    graph.AddResponse(source, target);
                    relationsCount++;
                } else if (relationType == 3 && !source.HasConditionTo(target, graph)) // condition
                {
                    if (!source.Equals(target) || doSelfConditions)
                    {
                        graph.AddCondition(source, target);
                        relationsCount++;
                    }
                }
            }
            return graph;
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
