using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //TestCopyMethod();
            //TestUniqueTracesMethod();

            var activities = new HashSet<LogEvent>();

            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                activities.Add(new LogEvent { Id = "" + ch, Name = "" });
            }

            var exAl = new ExhaustiveApproach(activities);

            while (true)
            {
                var input = Console.ReadLine();
                switch (input)
                {
                    case "STOP":
                        exAl.Stop();
                        break;
                    default:
                        exAl.AddEvent(input);
                        break;
                }


                Console.WriteLine(exAl.Graph);
            }
            // TODO: Read from log
            // TODO: Build Processes, LogTraces and LogEvents

            // TODO: Run main algorithm
        }

        private static void TestCopyMethod()
        {
            var dcrGraph = new DcrGraph();
            dcrGraph.Activities = new HashSet<Activity>();
            dcrGraph.Activities.Add(new Activity {Id = "A"});
            dcrGraph.Activities.Add(new Activity { Id = "B" });
            dcrGraph.Activities.Add(new Activity { Id = "C" });
            dcrGraph.IncludeExcludes = new Dictionary<Activity, Dictionary<Activity, bool>>();
            dcrGraph.IncludeExcludes.Add(new Activity { Id = "A" }, new Dictionary<Activity, bool> { {new Activity {Id = "B"}, true}, {new Activity {Id = "C"}, false} } );
            dcrGraph.Conditions = new Dictionary<Activity, HashSet<Activity>>();
            dcrGraph.Conditions.Add(new Activity {Id="A"}, new HashSet<Activity> { new Activity {Id="B"}, new Activity {Id="C"} });

            Console.WriteLine(dcrGraph);

            Console.WriteLine("--------------------------------------------------------------------------------");

            var copy = dcrGraph.Copy();
            Console.WriteLine(copy);
            Console.ReadKey();
        }

        private static void TestUniqueTracesMethod()
        {
            var dcrGraph = new DcrGraph();
            dcrGraph.Activities = new HashSet<Activity>();
            dcrGraph.Activities.Add(new Activity { Id = "A" });
            dcrGraph.Activities.Add(new Activity { Id = "B" });
            dcrGraph.Activities.Add(new Activity { Id = "C" });
            dcrGraph.IncludeExcludes = new Dictionary<Activity, Dictionary<Activity, bool>>();
            dcrGraph.IncludeExcludes.Add(new Activity { Id = "A" }, new Dictionary<Activity, bool> { { new Activity { Id = "B" }, true }, { new Activity { Id = "C" }, false } });
            dcrGraph.Conditions = new Dictionary<Activity, HashSet<Activity>>();
            dcrGraph.Conditions.Add(new Activity { Id = "A" }, new HashSet<Activity> { new Activity { Id = "B" }, new Activity { Id = "C" } });

            Console.WriteLine(dcrGraph);

            Console.WriteLine("--------------------------------------------------------------------------------");

            var traceFinder = new UniqueTraceFinder();
            var traces = traceFinder.GetUniqueTraces(dcrGraph);
            foreach (var logTrace in traces)
            {
                foreach (var logEvent in logTrace.Events)
                {
                    Console.Write(logEvent.Id);
                }
                Console.WriteLine();
            }
        }
    }
}
