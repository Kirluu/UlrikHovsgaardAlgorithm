using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlrikHovsgaardAlgorithm
{
    public class TestClassForCSharpStuff
    {
        public Dictionary<Activity, HashSet<Activity>> RedundantResponses { get; set; } = new Dictionary<Activity, HashSet<Activity>>();

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
                //a is excluded
                graph.SetIncluded(false, a.Id);
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
            unique.SupplyTracesToBeComparedTo(unique.GetUniqueTraces(graph));

            var copy = graph.Copy2();

            Console.WriteLine("Testing whether objects in both copies are changed:");
            var originalActivityB = graph.GetActivity("B");
            var activityB = copy.GetActivity("B");
            Console.WriteLine(originalActivityB.Executed + " " + activityB.Executed);
            originalActivityB.Executed = !originalActivityB.Executed;
            Console.WriteLine("Changed original...");
            Console.WriteLine(originalActivityB.Executed + " " + activityB.Executed);
            
            Dictionary<Activity, bool> targets;
            if (copy.IncludeExcludes.TryGetValue(activityB, out targets))
            {
                targets.Remove(copy.GetActivity("C"));
            }

            Console.WriteLine(unique.CompareTracesFoundWithSupplied(copy));

            Console.ReadLine();

            // Conclusion: Doesn't work
        }

        public void TestDictionaryStuff()
        {
            
        }
    }
}
