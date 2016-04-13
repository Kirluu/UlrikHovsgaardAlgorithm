using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Mining;
using UlrikHovsgaardAlgorithm.Parsing;
using UlrikHovsgaardAlgorithm.RedundancyRemoval;
using System.IO;
using System.Management.Instrumentation;
using System.Security.Cryptography;
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

        public void TestLogParserHospital()
        {
            var watch = new Stopwatch();
            watch.Start();
            var log = XmlParser.ParseHospitalLog(Properties.Resources.Hospital_log);
            Console.WriteLine("Finished parsing " + log.Traces.Count + " traces. Took: " + watch.Elapsed);
            foreach (var trace in log.Traces.First().Events)
            {
                Console.WriteLine("Example trace: " + log.Traces.First().Id);
                Console.Write("ID: " + trace.IdOfActivity + ", Name: " + trace.Name + "   |   ");
            }
            Console.ReadLine();
        }

        public void TestLogParserBpiChallenge2015()
        {
            var watch = new Stopwatch();
            watch.Start();
            var log = XmlParser.ParseLog(Properties.Resources.BPIC15_small);
            Console.WriteLine("Finished parsing " + log.Traces.Count + " traces. Took: " + watch.Elapsed);
            Console.ReadLine();
            var exhaustiveApproach = new ExhaustiveApproach(new HashSet<Activity>(log.Alphabet.Select(x => new Activity(x.IdOfActivity, x.Name))));
            foreach (var trace in log.Traces)
            {
                exhaustiveApproach.AddTrace(trace);
            }

            Console.WriteLine(log.Alphabet.Count);

            var list = log.Alphabet.ToList();
            list.Sort((a,b) => String.Compare(a.Name, b.Name, StringComparison.Ordinal));

            foreach (var lEvent in list)
            {
                Console.WriteLine(lEvent.Name);
            }

            //Console.WriteLine(exhaustiveApproach.Graph);
            Console.ReadLine();


            exhaustiveApproach.PostProcessing();

            Console.WriteLine(exhaustiveApproach.Graph);
            Console.ReadLine();


            Console.WriteLine(RedundancyRemover.RemoveRedundancy(exhaustiveApproach.Graph));
            Console.ReadLine();

        }

        public void TestDcrGraphXmlParserFromDcrGraphsNet()
        {
            var watch = new Stopwatch();
            watch.Start();
            var graph = XmlParser.ParseDcrGraph(Properties.Resources.DCRGraphNETtest);
            Console.WriteLine("Finished parsing graph in: " + watch.Elapsed);
            Console.WriteLine(graph);
            Console.ReadLine();
        }
        

        public void TestFlowerModel()
        {
            var graph = new DcrGraph();
            var trace = new LogTrace();

            for (char ch = 'A'; ch <= 'D'; ch++)
            {
                graph.Activities.Add(new Activity("" + ch, "somename " + ch) {Included = true});
                trace.AddEventsWithChars(ch);
            }

            var exhaustive =
                new ExhaustiveApproach(new HashSet<Activity>
                {
                    new Activity("A", "somenameA"),
                    new Activity("B", "somenameB"),
                    new Activity("C", "somenameC"),
                    new Activity("D", "somenameD")
                });
            exhaustive.Graph = graph;
            exhaustive.AddTrace(trace);

            Console.WriteLine(exhaustive.Graph);
            exhaustive.Graph = RedundancyRemover.RemoveRedundancy(exhaustive.Graph);
            Console.WriteLine(exhaustive.Graph);
            Console.WriteLine(QualityDimensionRetriever.Retrieve(exhaustive.Graph, new Log() {Traces = {trace}}));
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
            var traceId = 1000;
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'G'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }
            var inputLog = new Log();
            var currentTrace = new LogTrace() {Id = traceId++.ToString()};
            inputLog.Traces.Add(currentTrace);
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
                        currentTrace = new LogTrace() { Id = traceId++.ToString() };
                        inputLog.Traces.Add(currentTrace);
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
                    case "NESTED":
                        exAl.CreateNests();
                        break;
                    case "CHANGE TRACE":
                        inputLog.Traces.Add(currentTrace);
                        var newId = Console.ReadLine();
                        currentTrace = inputLog.Traces.Find(x => x.Id == newId) ?? new LogTrace() { Id = newId};

                        break;

                    default:
                        if (exAl.Graph.GetActivities().Any(a => a.Id == input))
                        {
                            exAl.AddEvent(input, currentTrace.Id);
                            currentTrace.Add(new LogEvent(input, "somename" + input) {EventId = "" + id++});
                        }
                        break;
                }


                Console.WriteLine("Current trace id: " + currentTrace.Id);
                Console.WriteLine(exAl.Graph);

                //the quality probably suffers because the traces contains unfinished traces. 
                Console.WriteLine(QualityDimensionRetriever.Retrieve(exAl.Graph, inputLog));
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
            var trace1 = new LogTrace('A', 'B', 'C', 'D');
            var trace2 = new LogTrace('A', 'B', 'C', 'D');

            Console.WriteLine(trace1.Equals(trace2));

            Console.ReadLine();

            // Conclusion: Use .Equals() method
        }

        public void TestAreUniqueTracesEqual()
        {
            var trace1 = new LogTrace('A', 'B', 'C', 'D');
            var trace2 = new LogTrace('A', 'C', 'C', 'D');

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

        public void TestOriginalLog()
        {
            var activities = new HashSet<Activity>();

            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                activities.Add(new Activity("" + ch, "somename " + ch));
            }

            var exAl = new ExhaustiveApproach(activities);

            

            var trace1 = new LogTrace();
            trace1.AddEventsWithChars('A', 'B', 'E');
            var trace2 = new LogTrace();
            trace2.AddEventsWithChars('A', 'C', 'F', 'A', 'B', 'B', 'F');
            var trace3 = new LogTrace();
            trace3.AddEventsWithChars('A', 'C', 'E');
            var trace4 = new LogTrace();
            trace4.AddEventsWithChars('A', 'D', 'F');
            var trace5 = new LogTrace();
            trace5.AddEventsWithChars('A', 'B', 'F', 'A', 'B', 'E');
            var trace6 = new LogTrace();
            trace6.AddEventsWithChars('A', 'C', 'F');
            var trace7 = new LogTrace();
            trace7.AddEventsWithChars('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E');
            var trace8 = new LogTrace();
            trace8.AddEventsWithChars('A', 'B', 'B', 'B', 'F');
            var trace9 = new LogTrace();
            trace9.AddEventsWithChars('A', 'B', 'B', 'E');
            var trace10 = new LogTrace();
            trace10.AddEventsWithChars('A', 'C', 'F', 'A', 'C', 'E');

            Log log = new Log() {Traces = {trace1, trace2, trace3, trace4, trace5, trace6, trace7, trace8, trace9, trace10}};
            
            exAl.AddLog(log);
            
            Console.WriteLine(exAl.Graph);
            Console.WriteLine(QualityDimensionRetriever.Retrieve(exAl.Graph,log));
            Console.ReadLine();

            exAl.Graph = RedundancyRemover.RemoveRedundancy(exAl.Graph);

            Console.WriteLine(exAl.Graph);
            Console.WriteLine(QualityDimensionRetriever.Retrieve(exAl.Graph, log));
            Console.ReadLine();

            exAl.PostProcessing();

            Console.WriteLine(exAl.Graph);
            Console.WriteLine(QualityDimensionRetriever.Retrieve(exAl.Graph, log));
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

            exAl.AddTrace(new LogTrace('A', 'B', 'E'));
            exAl.AddTrace(new LogTrace('A', 'C', 'F', 'A', 'B', 'B', 'F'));
            exAl.AddTrace(new LogTrace('A', 'C', 'E'));
            exAl.AddTrace(new LogTrace('A', 'D', 'F'));
            exAl.AddTrace(new LogTrace('A', 'B', 'F', 'A', 'B', 'E'));
            exAl.AddTrace(new LogTrace('A', 'C', 'F'));
            exAl.AddTrace(new LogTrace('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E'));
            exAl.AddTrace(new LogTrace('A', 'B', 'B', 'B', 'F'));
            exAl.AddTrace(new LogTrace('A', 'B', 'B', 'E'));
            exAl.AddTrace(new LogTrace('A', 'C', 'F', 'A', 'C', 'E'));

            Console.WriteLine(exAl.Graph);
            Console.ReadLine();
            var traceFinder = new UniqueTraceFinderWithComparison(exAl.Graph);
            var traces = traceFinder.GetUniqueTracesThreaded(exAl.Graph);
            Console.ReadLine();
        }

        public void FlowerTestSyncVsThreaded()
        {
            var graph = new DcrGraph();

            for (char ch = 'A'; ch <= 'G'; ch++)
            {
                graph.Activities.Add(new Activity("" + ch, "somename " + ch));
                graph.SetIncluded(true, "" + ch);
            }
            var traceFinder = new UniqueTraceFinderWithComparison(graph);
            var copy = graph.Copy();
            copy.AddActivity("H", "somenameH");
            copy.SetIncluded(true, "H");
            Console.ReadLine();
            var traces = traceFinder.CompareTracesFoundWithSuppliedThreaded(copy);
            //var traces = traceFinder.GetUniqueTracesThreaded(graph);
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

        public void TestRetrieveIncludeRelationTrust()
        {
            var someLog =
                new Log()
                {
                    Alphabet = new HashSet<LogEvent>
                    {
                        new LogEvent("A", "somenameA"),
                        new LogEvent("B", "somenameB"),
                        new LogEvent("C", "somenameC")
                    },
                    Traces =
                    new List<LogTrace>
                    {
                        new LogTrace('A', 'B', 'C'),
                        new LogTrace('A', 'C'),
                        new LogTrace('A', 'B', 'B')
        }
                };
            var retriever = new StatisticsRetriever(someLog);
            var trust = retriever.RetrieveIncludeRelationTrust();

            foreach (var keyVal in trust)
            {
                var source = keyVal.Key;
                foreach (var keyVal2 in keyVal.Value)
                {
                    Console.WriteLine("Trust " + source.Id + " -->+ " + keyVal2.Key.Id + " : " + keyVal2.Value);
                }
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
                new Log() { Traces = 
                    new List<LogTrace>
                    {
                        new LogTrace('A', 'B', 'C'),
                        new LogTrace('A', 'C')
                    }
                };
            var res = QualityDimensionRetriever.Retrieve(graph, someLog);
            Console.WriteLine(graph);
            Console.WriteLine(res);

            var graph2 = new DcrGraph();
            graph2.AddActivity("A", "somename1");
            graph2.AddActivity("B", "somename2");
            graph2.AddActivity("C", "somename3");
            graph2.SetIncluded(true, "A");
            graph2.SetIncluded(true, "B");
            graph2.SetIncluded(true, "C");

            res = QualityDimensionRetriever.Retrieve(graph2, someLog);
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

            res = QualityDimensionRetriever.Retrieve(graph3, someLog);
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
            originalLog.Add(new LogTrace('A', 'B', 'E'));
            originalLog.Add(new LogTrace('A', 'C', 'F', 'A', 'B', 'B', 'F'));
            originalLog.Add(new LogTrace('A', 'C', 'E'));
            originalLog.Add(new LogTrace('A', 'D', 'F'));
            originalLog.Add(new LogTrace('A', 'B', 'F', 'A', 'B', 'E'));
            originalLog.Add(new LogTrace('A', 'C', 'F'));
            originalLog.Add(new LogTrace('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E'));
            originalLog.Add(new LogTrace('A', 'B', 'B', 'B', 'F'));
            originalLog.Add(new LogTrace('A', 'B', 'B', 'E'));
            originalLog.Add(new LogTrace('A', 'C', 'F', 'A', 'C', 'E'));
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

            var log = new Log() {Traces = originalLog};

            res = QualityDimensionRetriever.Retrieve(exAl.Graph, log);
            Console.WriteLine(exAl.Graph);
            Console.WriteLine(res);

            Console.WriteLine("Removing redundancy::::::::::::::::::::::::::::::::::");
            exAl.Graph = RedundancyRemover.RemoveRedundancy(exAl.Graph);

            res = QualityDimensionRetriever.Retrieve(exAl.Graph, log);
            Console.WriteLine(exAl.Graph);
            Console.WriteLine(res);

            Console.ReadLine();
        }
    }
}
