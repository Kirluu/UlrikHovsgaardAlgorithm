using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using System.IO;
using UlrikHovsgaardAlgorithm.GraphSimulation;
using UlrikHovsgaardAlgorithm.QualityMeasures;

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

        # region Redundancy Test Cases

        public void RedundancyTestcasesAll()
        {
            RedundancyTestCase1();
            RedundancyTestCase2();
            RedundancyTestCase3();
            RedundancyTestCase4();
            RedundancyTestCase5();
            RedundancyTestCase6();
            RedundancyTestCase7();
        }

        //Include: Hvis B kun kan køres efter A(enten via inclusion eller condition) og både A og B har en include relation til C, kan “B->+C” altid slettes.
        public void RedundancyTestCase1()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") {Included = false};
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);
            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityC.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityC.Id);
            Console.WriteLine("The initial Test Case 1 graph before redundancy removal:");
            Console.WriteLine(dcrGraph);

            Console.WriteLine("Now one of the redundant include relations should be removed:");
            //TODO: Assert
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();
        }

        //Include: condition A->B er redundant med include A->B, hvis B ikke har andre indgående includes og A ikke kan blive ekskluderet. 
        public void RedundancyTestCase2()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = false };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            Console.WriteLine("The initial Test Case 2 graph before redundancy removal:");
            Console.WriteLine(dcrGraph);

            Console.WriteLine("\nNow the redundant condition relation should be removed:");
            //TODO: Assert
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();
        }

        //Include: Response og Exclude fra samme source til samme target er i udgangspunkt redundante, hvis target ikke har nogle indgående include relationer.
        public void RedundancyTestCase3()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddResponse(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);
            Console.WriteLine("The initial Test Case 3 graph before redundancy removal:");
            Console.WriteLine(dcrGraph);

            Console.WriteLine("\nNow the redundant response relation should be removed:");
            //TODO: Assert
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();
        }

        //Hvis en aktivitet er included og ikke har nogle indgående exclude-relationer, er indgående include-relationer redundante. 
        public void RedundancyTestCase4()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = true };
            var activityB = new Activity("B", "somename2") { Included = true };
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.AddIncludeExclude(true, activityA.Id, activityB.Id);
            Console.WriteLine("The initial Test Case 4 graph before redundancy removal:");
            Console.WriteLine(dcrGraph);

            Console.WriteLine("\nNow the redundant include relation should be removed:");
            //TODO: Assert
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();
        }

        //Condition og milestone er redundante med hinanden fra A til B, hvis A er excluded og altid bliver sat som pending, når den bliver inkluderet. 
        public void RedundancyTestCase5()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") {Included = false};
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddMileStone(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddResponse(activityC.Id, activityA.Id);
            Console.WriteLine("The initial Test Case 5 graph before redundancy removal:");
            Console.WriteLine(dcrGraph);

            Console.WriteLine("\nNow either the redundant Condition or Milestone relation should be removed:");
            //TODO: Assert
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();
        }

        //Ikke-included og ingen include relationer med den som target, gør det er redundant at den (og alle dens udadgående relationer) er medtaget i grafen. 
        public void RedundancyTestCase6()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false };
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddCondition(activityA.Id, activityB.Id);
            dcrGraph.AddMileStone(activityA.Id, activityB.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityC.Id);
            dcrGraph.AddResponse(activityA.Id, activityB.Id);
            dcrGraph.AddResponse(activityB.Id, activityA.Id);
            Console.WriteLine("The initial Test Case 6 graph before redundancy removal:");
            Console.WriteLine(dcrGraph);

            Console.WriteLine("\nNow all the relations from or to A should be removed:");
            //TODO: Assert
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();
        }

        //Hvis A er pending og excluded, og der er en response og include relation fra B til A, så er pending og response redundante med hinanden, hvis B ikke kan køres efter A. 
        public void RedundancyTestCase7()
        {
            var dcrGraph = new DcrGraph();

            var activityA = new Activity("A", "somename1") { Included = false, Pending = true};
            var activityB = new Activity("B", "somename2") { Included = true };
            var activityC = new Activity("C", "somename3") { Included = true };

            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);

            dcrGraph.AddResponse(activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityC.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(true, activityB.Id, activityA.Id);
            dcrGraph.AddIncludeExclude(false, activityA.Id, activityB.Id);
            Console.WriteLine("The initial Test Case 7 graph before redundancy removal:");
            Console.WriteLine(dcrGraph);

            Console.WriteLine("\nNow either the redundant response relation or A's initial pending state should be removed:");
            //TODO: Assert
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(dcrGraph));
            Console.ReadLine();
        }

        #endregion

        //TODO: test that non-redundant relations are not removed.

        public void TestFlowerModel()
        {
            var graph = new DcrGraph();
            for (char ch = 'A'; ch <= 'Z'; ch++)
            {
                graph.Activities.Add(new Activity("" + ch, "somename " + ch) {Included = true});
            }

            Console.WriteLine(graph);
            
            Console.WriteLine(RedundancyRemover.RemoveRedundancy(graph));

            Console.ReadLine();
        }

        public void TestAlmostFlowerModel()
        {
            var graph = new DcrGraph();
            for (char ch = 'A'; ch <= 'Z'; ch++)
            {
                graph.Activities.Add(new Activity("" + ch, "somename " + ch) { Included = true });
            }

            graph.AddResponse("A", "B");
            graph.AddIncludeExclude(true, "B", "C");
            graph.AddIncludeExclude(true, "C", "D");
            graph.AddIncludeExclude(false,"B", "B");
            Console.WriteLine(graph);

            Console.WriteLine(RedundancyRemover.RemoveRedundancy(graph));

            Console.ReadLine();
        }

        public void ExhaustiveTest()
        {
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'G'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }
            var traces = new List<LogTrace>();
            var currentTrace = new LogTrace();
            traces.Add(currentTrace);
            var exAl = new ExhaustiveApproach(activities);

            int id = 0;

            while (true)
            {
                var input = Console.ReadLine();
                switch (input)
                {
                    case "STOP":
                        exAl.Stop();
                        currentTrace = currentTrace.Copy();
                        traces.Add(currentTrace);
                        break;
                    case "AUTOLOG":
                        Console.WriteLine("Please input a termination index between 0 - 100 : \n");
                        var logGen = new LogGenerator9001(Convert.ToInt32(Console.ReadLine()), exAl.Graph);
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
                    case "POST":
                        exAl.PostProcessing();
                        break;
                    default:
                        exAl.AddEvent(input);
                        currentTrace.Add(new LogEvent() {IdOfActivity = input, EventId = "" + id++});
                        break;
                }


                Console.WriteLine(exAl.Graph);
                
                Console.WriteLine(QualityDimensionRetriever.RetrieveQualityDimensions(exAl.Graph, traces));
            }
        }

        public void TestCopyMethod()
        {
            var dcrGraph = new DcrGraph();
            var activityA = new Activity("A", "somename1");
            var activityB = new Activity("B", "somename2");
            var activityC = new Activity("C", "somename3");
            dcrGraph.Activities.Add(activityA);
            dcrGraph.Activities.Add(activityB);
            dcrGraph.Activities.Add(activityC);
            dcrGraph.IncludeExcludes.Add(activityA, new Dictionary<Activity, bool> { { activityB, true }, { activityC, false } });
            dcrGraph.Conditions.Add(activityA, new HashSet<Activity> { activityB, activityC });

            Console.WriteLine(dcrGraph);

            Console.WriteLine("--------------------------------------------------------------------------------");

            var copy = dcrGraph.Copy();
            Console.WriteLine(copy);
            Console.ReadKey();
        }

        //public void TestUniqueTracesMethod()
        //{
        //    var dcrGraph = new DcrGraph();
        //    dcrGraph.Activities = new HashSet<Activity>();
        //    var activityA = new Activity("A", "somename1");
        //    var activityB = new Activity("B", "somename2");
        //    var activityC = new Activity("C", "somename3");
        //    dcrGraph.Activities.Add(activityA);
        //    dcrGraph.Activities.Add(activityB);
        //    dcrGraph.Activities.Add(activityC);
        //    dcrGraph.IncludeExcludes.Add(activityA, new Dictionary<Activity, bool> { { activityB, true }, { activityC, false } });
        //    dcrGraph.Conditions.Add(activityA, new HashSet<Activity> { activityB, activityC });

        //    Console.WriteLine(dcrGraph);

        //    Console.WriteLine("--------------------------------------------------------------------------------");

        //    var traceFinder = new UniqueTraceFinderWithComparison(dcrGraph);
        //    var traces = traceFinder.GetUniqueTraces3(dcrGraph);
        //    foreach (var logTrace in traces)
        //    {
        //        foreach (var logEvent in logTrace.Events)
        //        {
        //            Console.Write(logEvent.IdOfActivity);
        //        }
        //        Console.WriteLine();
        //    }
        //}

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
            //RedundantResponses[source].Add(new Activity {EventId = "C"});
            //Console.WriteLine(".......");
            //foreach (var activity in RedundantResponses[source])
            //{
            //    Console.WriteLine(activity.EventId);
            //}

            Console.ReadLine();

            // Conclusion: HashSet works as intended when adding more activities with already existing activity of same "EventId" value
            // Have to check whether Dictionary entry already exists or not
        }

        public void TestAreTracesEqualSingle()
        {
            var trace1 = new LogTrace {Events = new List<LogEvent> { new LogEvent { IdOfActivity = "A"}, new LogEvent { IdOfActivity = "B" }, new LogEvent { IdOfActivity = "C" }, new LogEvent { IdOfActivity = "D" } } };
            var trace2 = new LogTrace { Events = new List<LogEvent> { new LogEvent { IdOfActivity = "A" }, new LogEvent { IdOfActivity = "B" }, new LogEvent { IdOfActivity = "C" }, new LogEvent { IdOfActivity = "D" } } };

            Console.WriteLine(trace1.Equals(trace2));

            Console.ReadLine();

            // Conclusion: Use .Equals() method
        }

        public void TestAreUniqueTracesEqual()
        {
            var trace1 = new LogTrace { Events = new List<LogEvent> { new LogEvent { IdOfActivity = "A" }, new LogEvent { IdOfActivity = "B" }, new LogEvent { IdOfActivity = "C" }, new LogEvent { IdOfActivity = "D" } } };
            var trace2 = new LogTrace { Events = new List<LogEvent> { new LogEvent { IdOfActivity = "A" }, new LogEvent { IdOfActivity = "C" }, new LogEvent { IdOfActivity = "C" }, new LogEvent { IdOfActivity = "D" } } };

            var traces1 = new List<LogTrace> { trace1, trace2, trace1 };
            var traces2 = new List<LogTrace> { trace1, trace2 };

            Console.WriteLine(UniqueTraceFinderWithComparison.AreUniqueTracesEqual(traces1, traces2));

            Console.ReadLine();

            // Conclusion: Works perfectly with sorting assumption
            // Conclusion2: If we can, avoid sorting-necessity - assume sorted behavior --> Better code
        }

        //public void TestCompareTracesWithSupplied()
        //{
        //    var activities = new HashSet<Activity> { new Activity("A", "somename1"), new Activity("B", "somename2"), new Activity("C", "somename3") };
        //    var graph = new DcrGraph();

        //    foreach (var a in activities)
        //    {
        //        graph.AddActivity(a.Id, a.Name);
        //    }

        //    graph.SetIncluded(true, "A"); // Start at A

        //    graph.AddIncludeExclude(true, "A", "B");
        //    graph.AddIncludeExclude(true, "A", "C");
        //    graph.AddIncludeExclude(true, "B", "C");

        //    graph.AddIncludeExclude(false, "C", "B");
        //    graph.AddIncludeExclude(false, "A", "A");
        //    graph.AddIncludeExclude(false, "B", "B");
        //    graph.AddIncludeExclude(false, "C", "C");

        //    var unique = new UniqueTraceFinderWithComparison(graph);

        //    var copy = graph.Copy(); // Verified Copy works using Activity level copying

        //    // Remove B -->+ C (Gives same traces :) )
        //    var activityB = copy.GetActivity("B");
        //    Dictionary<Activity, bool> targets;
        //    if (copy.IncludeExcludes.TryGetValue(activityB, out targets))
        //    {
        //        targets.Remove(copy.GetActivity("C"));
        //    }

        //    // Remove A -->+ C (Gives different traces :) )
        //    //var activityA = copy.GetActivity("A");
        //    //Dictionary<Activity, bool> targets;
        //    //if (copy.IncludeExcludes.TryGetValue(activityA, out targets))
        //    //{
        //    //    targets.Remove(copy.GetActivity("C"));
        //    //}

        //    Console.WriteLine(unique.CompareTracesFoundWithSupplied3(copy));

        //    Console.ReadLine();

        //    // Conclusion: I do believe it works!
        //}

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

            var utf = new UniqueTraceFinderWithComparison(graph);
            foreach (var logTrace in utf.GetUniqueTraces(graph))
            {
                foreach (var logEvent in logTrace.Events)
                {
                    Console.Write(logEvent.IdOfActivity);
                }
                Console.WriteLine();
            }

            Console.ReadLine();
        }

        public void TestExportDcrGraphToXml()
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

            var xml = graph.ExportToXml();
            Console.WriteLine(xml);

            File.WriteAllText("E:/DCR2XML.xml", xml);

            Console.ReadLine();
        }

        public void TestOutputGraphWithOriginalTestLog()
        {
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }

            var exAl = new ExhaustiveApproach(activities);

            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'C', 'F', 'A', 'B', 'B', 'F'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'C', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'D', 'F'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'F', 'A', 'B', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'C', 'F'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'B', 'B', 'F'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'B', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'C', 'F', 'A', 'C', 'E'));

            Console.WriteLine(exAl.Graph);
            Console.ReadLine();
            exAl.Graph = RedundancyRemover.RemoveRedundancy(exAl.Graph);
            Console.WriteLine(exAl.Graph);
            Console.ReadLine();
            exAl.PostProcessing();
            Console.WriteLine(exAl.Graph);
            Console.ReadLine();
        }

        public void TestThreadedTraceFindingWithOriginalTestLog()
        {
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }

            var exAl = new ExhaustiveApproach(activities);

            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'C', 'F', 'A', 'B', 'B', 'F'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'C', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'D', 'F'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'F', 'A', 'B', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'C', 'F'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'B', 'B', 'F'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'B', 'B', 'E'));
            exAl.AddTrace(new LogTrace().AddEventsWithChars('A', 'C', 'F', 'A', 'C', 'E'));

            Console.WriteLine(exAl.Graph);
            Console.ReadLine();
            var traceFinder = new UniqueTraceFinderWithComparison(exAl.Graph);
            var traces = traceFinder.GetUniqueTracesThreaded(exAl.Graph);
            Console.ReadLine();
        }

        public void RedundancyRemoverStressTest()
        {
            var graph = new DcrGraph();
            for (char ch = 'A'; ch <= 'K'; ch++)
            {
                graph.AddActivity(ch.ToString(), "somename" + ch);
                graph.SetIncluded(true, ch.ToString());
            }

            Console.ReadLine();
        }

        public void TestSimpleGraph()
        {
            var graph = new DcrGraph();
            graph.AddActivity("A", "somename1");
            graph.AddActivity("B", "somename2");

            graph.SetIncluded(true, "A"); // Start at A
            
            graph.SetIncluded(true, "B");

            Console.WriteLine(graph);

            var graph2 = RedundancyRemover.RemoveRedundancy(graph);

            Console.WriteLine(graph2);

            Console.ReadLine();
        }

        public void TestFinalStateMisplacement()
        {
            var graph = new DcrGraph();
            graph.AddActivity("A", "somename1");
            graph.AddActivity("B", "somename2");
            graph.AddActivity("C", "somename3");

            graph.SetIncluded(true, "A"); // Start at A
            
            graph.AddIncludeExclude(true, "A", "B");
            graph.AddIncludeExclude(true, "B", "C");
            graph.AddIncludeExclude(false, "C", "B");
            graph.AddResponse("B", "C");
            // Self-excludes
            graph.AddIncludeExclude(false, "A", "A");
            graph.AddIncludeExclude(false, "B", "B");
            graph.AddIncludeExclude(false, "C", "C");

            
            Console.WriteLine(graph);

            var graph2 = RedundancyRemover.RemoveRedundancy(graph);
            

            Console.WriteLine(graph2);

            Console.ReadLine();
        }

        public void TestActivityCreationLimitations()
        {
            // Shouldn't crash:
            var act = new Activity("A", "somename");
            Console.WriteLine(act);

            // Should fail:
            try
            {
                act = new Activity("A;2", "somename");
            }
            catch
            {
                Console.WriteLine("Fail: " + act);
            }

            // Should work:
            act = new Activity("A323fgfdå", "somename");
            Console.WriteLine(act);

            Console.ReadLine();
        }

        public void TestCanActivityEverBeIncluded()
        {
            var graph = new DcrGraph();
            graph.AddActivity("A", "somename1");
            graph.AddActivity("B", "somename2");
            graph.AddActivity("C", "somename3");
            graph.AddActivity("D", "somename3");

            graph.SetIncluded(true, "A"); // Start at A

            graph.AddIncludeExclude(true, "A", "B");
            graph.AddIncludeExclude(true, "B", "C");
            graph.AddIncludeExclude(false, "C", "B");
            graph.AddResponse("B", "C");
            // Self-excludes
            graph.AddIncludeExclude(false, "A", "A");
            graph.AddIncludeExclude(false, "B", "B");
            graph.AddIncludeExclude(false, "C", "C");

            Console.WriteLine(graph);

            foreach (var activity in graph.Activities)
            {
                Console.WriteLine(activity.Id + " includable: " + graph.CanActivityEverBeIncluded(activity.Id));
            }

            Console.ReadLine();
        }

        public void TestQualityDimensionsRetriever()
        {
            var graph = new DcrGraph();
            graph.AddActivity("A", "somename1");
            graph.AddActivity("B", "somename2");
            graph.AddActivity("C", "somename3");
            //graph.AddActivity("D", "somename3");

            graph.SetIncluded(true, "A"); // Start at A

            graph.AddIncludeExclude(true, "A", "B");
            graph.AddIncludeExclude(true, "B", "C");
            graph.AddIncludeExclude(false, "C", "B");
            graph.AddResponse("B", "C");
            // Self-excludes
            graph.AddIncludeExclude(false, "A", "A");
            graph.AddIncludeExclude(false, "B", "B");
            graph.AddIncludeExclude(false, "C", "C");
            graph.AddCondition("A", "B");
            graph.AddMileStone("A", "B");

            var someLog  =
                new List<LogTrace>
                {
                    new LogTrace
                    {
                        Events =
                            new List<LogEvent>
                            {
                                new LogEvent {IdOfActivity = "A"},
                                new LogEvent {IdOfActivity = "B"},
                                new LogEvent {IdOfActivity = "C"}
                            }
                    },
                    new LogTrace
                    {
                        Events = 
                            new List<LogEvent>
                            {
                                new LogEvent {IdOfActivity = "A"},
                                new LogEvent {IdOfActivity = "C"}
                            }
                    }
            };
            var res = QualityDimensionRetriever.RetrieveQualityDimensions(graph, someLog);
            Console.WriteLine(graph);
            Console.WriteLine(res);

            var graph2 = new DcrGraph();
            graph2.AddActivity("A", "somename1");
            graph2.AddActivity("B", "somename2");
            graph2.AddActivity("C", "somename3");
            graph2.SetIncluded(true, "A");
            graph2.SetIncluded(true, "B");
            graph2.SetIncluded(true, "C");

            res = QualityDimensionRetriever.RetrieveQualityDimensions(graph2, someLog);
            Console.WriteLine(graph2);
            Console.WriteLine(res);

            var graph3 = new DcrGraph();
            graph3.AddActivity("A", "somename1");
            graph3.AddActivity("B", "somename2");
            graph3.AddActivity("C", "somename3");
            graph3.AddIncludeExclude(false, "A", "A");
            graph3.AddIncludeExclude(false, "B", "B");
            graph3.AddIncludeExclude(false, "C", "C");
            graph3.AddIncludeExclude(false, "A", "B");
            graph3.AddIncludeExclude(false, "B", "A");
            graph3.AddIncludeExclude(false, "C", "A");
            graph3.AddIncludeExclude(false, "C", "B");
            graph3.AddIncludeExclude(false, "A", "C");
            graph3.AddIncludeExclude(false, "B", "C");
            graph3.AddResponse("A", "B");
            graph3.AddResponse("A", "C");
            graph3.AddResponse("B", "A");
            graph3.AddResponse("B", "C");
            graph3.AddResponse("C", "A");
            graph3.AddResponse("C", "B");
            graph3.AddCondition("A", "B");
            graph3.AddCondition("A", "C");
            graph3.AddCondition("B", "A");
            graph3.AddCondition("B", "C");
            graph3.AddCondition("C", "A");
            graph3.AddCondition("C", "B");
            graph3.AddMileStone("A", "B");
            graph3.AddMileStone("A", "C");
            graph3.AddMileStone("B", "A");
            graph3.AddMileStone("B", "C");
            graph3.AddMileStone("C", "A");
            graph3.AddMileStone("C", "B");

            res = QualityDimensionRetriever.RetrieveQualityDimensions(graph3, someLog);
            Console.WriteLine(graph3);
            Console.WriteLine(res);

            // "Original" test log
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }

            var exAl = new ExhaustiveApproach(activities);

            var originalLog = new List<LogTrace>();
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'B', 'E'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'C', 'F', 'A', 'B', 'B', 'F'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'C', 'E'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'D', 'F'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'B', 'F', 'A', 'B', 'E'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'C', 'F'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'B', 'B', 'B', 'F'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'B', 'B', 'E'));
            originalLog.Add(new LogTrace().AddEventsWithChars('A', 'C', 'F', 'A', 'C', 'E'));
            exAl.AddTrace(originalLog[0]);
            exAl.AddTrace(originalLog[1]);
            exAl.AddTrace(originalLog[2]);
            exAl.AddTrace(originalLog[3]);
            exAl.AddTrace(originalLog[4]);
            exAl.AddTrace(originalLog[5]);
            exAl.AddTrace(originalLog[6]);
            exAl.AddTrace(originalLog[7]);
            exAl.AddTrace(originalLog[8]);
            exAl.AddTrace(originalLog[9]);

            res = QualityDimensionRetriever.RetrieveQualityDimensions(exAl.Graph, originalLog);
            Console.WriteLine(exAl.Graph);
            Console.WriteLine(res);

            Console.WriteLine("Removing redundancy::::::::::::::::::::::::::::::::::");
            exAl.Graph = RedundancyRemover.RemoveRedundancy(exAl.Graph);

            res = QualityDimensionRetriever.RetrieveQualityDimensions(exAl.Graph, originalLog);
            Console.WriteLine(exAl.Graph);
            Console.WriteLine(res);

            Console.ReadLine();
        }
    }
}
