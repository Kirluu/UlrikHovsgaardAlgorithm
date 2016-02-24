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
            var tester = new TestClassForCSharpStuff();
            //tester.TestDictionaryAccessAndAddition();
            //tester.TestAreTracesEqualSingle();
            //tester.TestAreUniqueTracesEqual();
            tester.TestCompareTracesWithSupplied();

            //var activities = new HashSet<Activity>();

            //for (char ch = 'A'; ch <= 'F'; ch++)
            //{
            //    activities.Add(new Activity() { Id = "" + ch});
            //}

            //var exAl = new ExhaustiveApproach(activities);

            //LogGenerator9001 logGen;

            //while(true)
            //{
            //    var input = Console.ReadLine();
            //    switch (input)
            //    {
            //        case "STOP":
            //            exAl.Stop();
            //            break;
            //        case "AUTOLOG":
            //            Console.WriteLine("Please input a termination index between 0 - 100 : \n");
            //            logGen = new LogGenerator9001(Convert.ToInt32(Console.ReadLine()), exAl.Graph);
            //            Console.WriteLine("Please input number of desired traces to generate : \n");
            //            List<LogTrace> log = logGen.GenerateLog(Convert.ToInt32(Console.ReadLine()));
            //            foreach (var trace in log)
            //            {
            //                Console.WriteLine(trace);
            //            }
            //            break;
            //        default:
            //            exAl.AddEvent(input);
            //            break;
            //    }


            //    Console.WriteLine(exAl.Graph);
            //}
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

            var traceFinder = new UniqueTraceFinderWithComparison();
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
