using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;

namespace UlrikHovsgaardAlgorithm
{
    public class TestClassForCSharpStuff
    {
        public Dictionary<Activity, HashSet<Activity>> RedundantResponses { get; set; } = new Dictionary<Activity, HashSet<Activity>>();

        //TODO: move to unit-test
        public void TestUnhealthyInput()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1");
            var activityB = new Activity("B", "somename2");
            var activityC = new Activity("C", "somename3");
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);
            dcrGraph.AddResponse(activityA.Id, activityB.Id);
            dcrGraph.AddResponse(activityB.Id, activityC.Id);
            dcrGraph.AddResponse(activityC.Id, activityA.Id);

            dcrGraph.SetPending(true,activityA.Id);

            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();

        }

        public void ExhaustiveTest()
        {
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }

            var exAl = new ExhaustiveApproach(activities);

            LogGenerator9001 logGen;

            while (true)
            {
                var input = Console.ReadLine();
                switch (input)
                {
                    case "STOP":
                        exAl.Stop();
                        break;
                    case "AUTOLOG":
                        Console.WriteLine("Please input a termination index between 0 - 100 : \n");
                        logGen = new LogGenerator9001(Convert.ToInt32(Console.ReadLine()), exAl.Graph);
                        Console.WriteLine("Please input number of desired traces to generate : \n");
                        List<LogTrace> log = logGen.GenerateLog(Convert.ToInt32(Console.ReadLine()));
                        foreach (var trace in log)
                        {
                            Console.WriteLine(trace);
                        }
                        break;
                    case "REDUNDANCY":
                        exAl.Graph = RedundancyRemover.RemoveRedundancy(exAl.Graph);
                        break;
                    default:
                        exAl.AddEvent(input);
                        break;
                }


                Console.WriteLine(exAl.Graph);
            }
        }

        public void TestCopyMethod()
        {
            var dcrGraph = new DcrGraph {Activities = new HashSet<Activity>()};
            var activityA = new Activity("A", "somename1");
            var activityB = new Activity("B", "somename2");
            var activityC = new Activity("C", "somename3");
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);
            dcrGraph.IncludeExcludes = new Dictionary<Activity, Dictionary<Activity, bool>>(); //TODO: use methods to add relations.
            dcrGraph.IncludeExcludes.Add(activityA, new Dictionary<Activity, bool> { { activityB, true }, { activityC, false } });
            dcrGraph.Conditions = new Dictionary<Activity, HashSet<Activity>>();
            dcrGraph.Conditions.Add(activityA, new HashSet<Activity> { activityB, activityC });

            Console.WriteLine(dcrGraph);

            Console.WriteLine("--------------------------------------------------------------------------------");

            var copy = dcrGraph.Copy();
            Console.WriteLine(copy);
            Console.ReadKey();
        }

        public void TestUniqueTracesMethod()
        {
            var dcrGraph = new DcrGraph();
            dcrGraph.Activities = new HashSet<Activity>();
            var activityA = new Activity("A", "somename1");
            var activityB = new Activity("B", "somename2");
            var activityC = new Activity("C", "somename3");
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);
            dcrGraph.IncludeExcludes = new Dictionary<Activity, Dictionary<Activity, bool>>();
            dcrGraph.IncludeExcludes.Add(activityA, new Dictionary<Activity, bool> { { activityB, true }, { activityC, false } });
            dcrGraph.Conditions = new Dictionary<Activity, HashSet<Activity>>();
            dcrGraph.Conditions.Add(activityA, new HashSet<Activity> { activityB, activityC });

            Console.WriteLine(dcrGraph);

            Console.WriteLine("--------------------------------------------------------------------------------");

            var traceFinder = new UniqueTraceFinderWithComparison();
            var traces = traceFinder.GetUniqueTraces3(dcrGraph);
            foreach (var logTrace in traces)
            {
                foreach (var logEvent in logTrace.Events)
                {
                    Console.Write(logEvent.Id);
                }
                Console.WriteLine();
            }
        }

        public void TestDictionaryAccessAndAddition()
        {
            var source = new Activity("A", "somename1");
            var target = new Activity("B", "somename2");
            var target2 = new Activity("C", "somename2");
            HashSet<Activity> targets;
            RedundantResponses.TryGetValue(source, out targets);
            if (targets == null)
            {
                RedundantResponses.Add(source, new HashSet<Activity> { target });
            }
            else
            {
                RedundantResponses[source].Add(target);
            }

            foreach (var activity in RedundantResponses[source])
            {
                Console.WriteLine(activity.Id);
            }

            Console.WriteLine("------------");

            RedundantResponses.TryGetValue(source, out targets);
            if (targets == null)
            {
                RedundantResponses.Add(source, new HashSet<Activity> { target2 });
            }
            else
            {
                RedundantResponses[source].Add(target2);
            }

            foreach (var activity in RedundantResponses[source])
            {
                Console.WriteLine(activity.Id);
            }
            //RedundantResponses[source].Add(new Activity {Id = "C"});
            //Console.WriteLine(".......");
            //foreach (var activity in RedundantResponses[source])
            //{
            //    Console.WriteLine(activity.Id);
            //}

            Console.ReadLine();

            // Conclusion: HashSet works as intended when adding more activities with already existing activity of same "Id" value
            // Have to check whether Dictionary entry already exists or not
        }

        public void TestAreTracesEqualSingle()
        {
            var trace1 = new LogTrace {Events = new List<LogEvent> { new LogEvent {Id = "A"}, new LogEvent { Id = "B" }, new LogEvent { Id = "C" }, new LogEvent { Id = "D" } } };
            var trace2 = new LogTrace { Events = new List<LogEvent> { new LogEvent { Id = "A" }, new LogEvent { Id = "B" }, new LogEvent { Id = "C" }, new LogEvent { Id = "D" } } };

            Console.WriteLine(UniqueTraceFinderWithComparison.AreTracesEqualSingle(trace1, trace2));

            Console.ReadLine();

            // Conclusion: Use .Equals() method
        }

        public void TestAreUniqueTracesEqual()
        {
            var trace1 = new LogTrace { Events = new List<LogEvent> { new LogEvent { Id = "A" }, new LogEvent { Id = "B" }, new LogEvent { Id = "C" }, new LogEvent { Id = "D" } } };
            var trace2 = new LogTrace { Events = new List<LogEvent> { new LogEvent { Id = "A" }, new LogEvent { Id = "C" }, new LogEvent { Id = "C" }, new LogEvent { Id = "D" } } };

            var traces1 = new List<LogTrace> { trace1, trace2, trace1 };
            var traces2 = new List<LogTrace> { trace1, trace2 };

            Console.WriteLine(UniqueTraceFinderWithComparison.AreUniqueTracesEqual(traces1, traces2));

            Console.ReadLine();

            // Conclusion: Works perfectly with sorting assumption
            // Conclusion2: If we can, avoid sorting-necessity - assume sorted behavior --> Better code
        }

        public void TestCompareTracesWithSupplied()
        {
            var activities = new HashSet<Activity> { new Activity("A", "somename1"), new Activity("B", "somename2"), new Activity("C", "somename3") };
            var graph = new DcrGraph();

            foreach (var a in activities)
            {
                graph.AddActivity(a.Id, a.Name);
            }

            graph.SetIncluded(true, "A"); // Start at A

            graph.AddIncludeExclude(true, "A", "B");
            graph.AddIncludeExclude(true, "A", "C");
            graph.AddIncludeExclude(true, "B", "C");

            graph.AddIncludeExclude(false, "C", "B");
            graph.AddIncludeExclude(false, "A", "A");
            graph.AddIncludeExclude(false, "B", "B");
            graph.AddIncludeExclude(false, "C", "C");

            var unique = new UniqueTraceFinderWithComparison();
            var uniqueTraces = unique.GetUniqueTraces3(graph);
            unique.SupplyTracesToBeComparedTo(uniqueTraces);

            var copy = graph.Copy2(); // Verified Copy2 works using Activity level copying

            // Remove B -->+ C (Gives same traces :) )
            var activityB = copy.GetActivity("B");
            Dictionary<Activity, bool> targets;
            if (copy.IncludeExcludes.TryGetValue(activityB, out targets))
            {
                targets.Remove(copy.GetActivity("C"));
            }

            // Remove A -->+ C (Gives different traces :) )
            //var activityA = copy.GetActivity("A");
            //Dictionary<Activity, bool> targets;
            //if (copy.IncludeExcludes.TryGetValue(activityA, out targets))
            //{
            //    targets.Remove(copy.GetActivity("C"));
            //}

            Console.WriteLine(unique.CompareTracesFoundWithSupplied3(copy));

            Console.ReadLine();

            // Conclusion: I do believe it works!
        }

        public void TestRedundancyRemover()
        {
            var activities = new HashSet<Activity> { new Activity("A", "somename1"), new Activity("B", "somename2"), new Activity("C", "somename3") };
            var graph = new DcrGraph();

            foreach (var a in activities)
            {
                graph.AddActivity(a.Id, a.Name);
            }

            graph.SetIncluded(true, "A"); // Start at A

            graph.AddIncludeExclude(true, "A", "B");
            graph.AddIncludeExclude(true, "A", "C");
            graph.AddIncludeExclude(true, "B", "C");

            graph.AddIncludeExclude(false, "C", "B");
            graph.AddIncludeExclude(false, "A", "A");
            graph.AddIncludeExclude(false, "B", "B");
            graph.AddIncludeExclude(false, "C", "C");

            Console.WriteLine(graph);

            //graph.Running = true;
            //graph.Execute(graph.GetActivity("A"));
            //graph.Execute(graph.GetActivity("C"));
            //Console.WriteLine(graph);

            Console.WriteLine("------------------");

            Console.WriteLine(RedundancyRemover.RemoveRedundancy(graph));

            Console.ReadLine();
        }

        public void TestRedundancyRemoverLimited()
        {
            var graph = new DcrGraph();
            graph.AddActivity("A", "somename1");

            graph.SetIncluded(true, "A"); // Start at A

            graph.AddIncludeExclude(false, "A", "A");

            Console.WriteLine(graph);

            //graph.Running = true;
            //graph.Execute(graph.GetActivity("A"));
            //graph.Execute(graph.GetActivity("C"));

            //Console.WriteLine(graph);

            Console.WriteLine("------------------");

            Console.WriteLine(RedundancyRemover.RemoveRedundancy(graph));

            Console.ReadLine();

            // Conclusion: Edits made to not see self-exclusion as redundant
        }

        public void TestRedundancyRemoverExcludes()
        {
            var graph = new DcrGraph();
            graph.AddActivity("A", "somename1");
            graph.AddActivity("B", "somename2");

            graph.SetIncluded(true, "A"); // Start at A
            graph.SetIncluded(true, "B"); // Start at B

            // If you choose A - cannot do B, if you choose B, can still do A.
            graph.AddIncludeExclude(false, "A", "B");
            // Self-excludes
            //graph.AddIncludeExclude(false, "A", "A");
            //graph.AddIncludeExclude(false, "B", "B");

            Console.WriteLine(graph);

            //graph.Running = true;
            //graph.Execute(graph.GetActivity("A"));
            //graph.Execute(graph.GetActivity("C"));

            //Console.WriteLine(graph);

            Console.WriteLine("------------------");

            Console.WriteLine(RedundancyRemover.RemoveRedundancy(graph));

            Console.ReadLine();

            // Conclusion: Edits made to not see self-exclusion as redundant
        }

        public void TestUniqueTracesMethodExcludes()
        {
            var graph = new DcrGraph();
            graph.AddActivity("A", "somename1");
            graph.AddActivity("B", "somename2");

            graph.SetIncluded(true, "A"); // Start at A
            graph.SetIncluded(true, "B"); // Start at B

            // If you choose A - cannot do B, if you choose B, can still do A.
            graph.AddIncludeExclude(false, "A", "B");
            // Self-excludes
            graph.AddIncludeExclude(false, "A", "A");
            graph.AddIncludeExclude(false, "B", "B");

            Console.WriteLine(graph);

            //graph.Running = true;
            //graph.Execute(graph.GetActivity("A"));
            //graph.Execute(graph.GetActivity("C"));

            //Console.WriteLine(graph);

            Console.WriteLine("------------------");

            var traces = new UniqueTraceFinderWithComparison().GetUniqueTraces(graph);
            foreach (var logTrace in traces)
            {
                foreach (var logEvent in logTrace.Events)
                {
                    Console.Write(logEvent.Id);
                }
                Console.WriteLine();
            }

            Console.ReadLine();
        }
    }
}
