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
        public static IEnumerable<DcrGraphSimple> Generate(int alphabetSize, int relationsCap, int numberOfGraphs)
        {
            var names = new List<string>();
            for (int i = 0; i < alphabetSize; i++)
                names.Add("" + i);

            var rand = new Random();
            var all = Enumerable.Range(0, numberOfGraphs).Select(x => GenerateRandomActivities(rand, names));
            return all.Select(x => GenerateGraphs(rand, x, relationsCap));
        }

        public static IEnumerable<DcrGraphSimple> Generate(int alphabetSize, int relationsCap, int numberOfGraphs, Func<DcrGraphSimple, bool> validator)
        {
            var names = new List<string>();
            for (int i = 0; i < alphabetSize; i++)
                names.Add("" + i);

            var rand = new Random();

            var graphs = new List<DcrGraphSimple>(numberOfGraphs);
            while (graphs.Count < numberOfGraphs)
            {
                var activities = GenerateRandomActivities(rand, names);
                var graph = GenerateGraphs(rand, activities, relationsCap);
                if (validator(graph)) 
                    graphs.Add(graph);
            }
            
            return graphs;
        }

        public static DcrGraphSimple GenerateGraphs(Random rand, IEnumerable<Activity> activities, int relationsCap)
        {
            var graph = new DcrGraphSimple(new HashSet<Activity>(activities));

            var relationsCount = 0;
            var activitiesList = activities.ToList();
            var numberOfActivities = activitiesList.Count;
            while (relationsCount < relationsCap)
            {
                var source = activitiesList[rand.Next(numberOfActivities)];
                var target = activitiesList[rand.Next(numberOfActivities)];

                var relationType = rand.Next(4);

                if (relationType == 0 && !(source.HasIncludeTo(target, graph) || source.HasExcludeTo(target, graph))) // include
                {
                    graph.AddInclude(source, target);
                    relationsCount++;
                } else if (relationType == 1 && !(source.HasExcludeTo(target, graph) || source.HasIncludeTo(target, graph))) //exclude
                {
                    graph.AddExclude(source, target);
                    relationsCount++;
                } else if (relationType == 2 && !source.HasResponseTo(target, graph)) // response
                {
                    graph.AddResponse(source, target);
                    relationsCount++;
                } else if (relationType == 3 && !source.HasConditionTo(target, graph)) // condition
                {
                    graph.AddCondition(source, target);
                    relationsCount++;
                }            
            }

            Console.WriteLine("Generated graph");
            return graph;
        }

        public static IEnumerable<Activity> GenerateRandomActivities(Random rand, List<string> names)
        {
            return names.Select(name => Tuple2Activity(name, (rand.Next(1) == 0, rand.Next(1) == 0, rand.Next(1) == 0)));
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
