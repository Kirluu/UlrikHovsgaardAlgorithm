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
using System.Windows.Forms;
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

            Console.WriteLine(new RedundancyRemover().RemoveRedundancy(dcrGraph));
            Console.ReadLine();

        }

        public void TestLogParserHospital()
        {
            var watch = new Stopwatch();
            watch.Start();
            var log =
                XmlParser.ParseLog(
                                new LogStandard("http://www.xes-standard.org/", "trace",
                                    new LogStandardEntry(DataType.String, "conceptName"), "event",
                                    new LogStandardEntry(DataType.String, "ActivityCode"),
                                    new LogStandardEntry(DataType.String, "conceptName"),
                                    new LogStandardEntry(DataType.String, "org:group")), Properties.Resources.Hospital_log);
            Console.WriteLine("Finished parsing " + log.Traces.Count + " traces. Took: " + watch.Elapsed);
            Console.WriteLine("Alphabeth of size " + log.Alphabet.Count);

            int occurences = 0;

            //
            log.Traces = new List<LogTrace>(log.Traces.Where(t => t.Events.Distinct().Count() < 8));

            foreach (var character in log.Alphabet)
            {
                foreach (var other in log.Alphabet.Where(a => (a.IdOfActivity == character.IdOfActivity && a.Name != character.Name)))
                {
                    //Console.WriteLine("Name: " + character.Name + ", " + character.IdOfActivity +" is not : "+ other.IdOfActivity);
                    occurences++;
                }
            }
            Console.WriteLine("occurences of Id/Name-mismatch: " + occurences);


            var actors = new HashSet<String>();
            foreach (var name in log.Traces.SelectMany(trace => trace.Events.Select(a => a.ActorName)))
            {
                actors.Add(name);
            }

            foreach (var name in actors)
            {
                var tracesLength = log.Traces.Where(t => t.Events.Any(n => n.ActorName == name)).Select(a=> a.Events.Count);

                Console.WriteLine(name + " :  " + tracesLength.Count() + " traces, Longest trace = " + tracesLength.Max());
            }
            
            foreach (var trace in log.Traces.First().Events)
            {
                //Console.WriteLine("Example trace: " + log.Traces.First().Id);
                //Console.Write("ID: " + trace.IdOfActivity + ", Name: " + trace.Name + "   |   ");
            }

            Console.WriteLine("\nPlease choose department to process mine:");
            string department= Console.ReadLine();

            var newLog = log.FilterByActor(department);

            ContradictionApproach ex = new ContradictionApproach(new HashSet<Activity>(newLog.Alphabet.Select(logEvent => new Activity(logEvent.IdOfActivity,logEvent.Name))));

            Console.WriteLine(ex.Graph);

            var redundancy = new RedundancyRemover();

            foreach (var trace in newLog.Traces)
            {
                ex.AddTrace(trace);
            }
            var redundancyRemoved = redundancy.RemoveRedundancy(ex.Graph);
            Console.WriteLine(redundancyRemoved);
            Console.WriteLine(QualityDimensionRetriever.Retrieve(redundancyRemoved, newLog));

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
                new ContradictionApproach(new HashSet<Activity>
                {
                    new Activity("A", "somenameA"),
                    new Activity("B", "somenameB"),
                    new Activity("C", "somenameC"),
                    new Activity("D", "somenameD")
                });
            exhaustive.Graph = graph;
            exhaustive.AddTrace(trace);

            Console.WriteLine(exhaustive.Graph);
            exhaustive.Graph = new RedundancyRemover().RemoveRedundancy(exhaustive.Graph);
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

            Console.WriteLine(new RedundancyRemover().RemoveRedundancy(graph));

            Console.ReadLine();
        }

        public void ParseMortgageApplication()
        {
            var graph = new DcrGraph();

            graph.AddActivities(new Activity("Collect Documents", "Collect Documents") {Included = true, Roles = "Caseworker"});

            graph.AddActivities(new Activity("Irregular neighbourhood", "Irregular neighbourhood") { Included = true, Roles = "it" });

            graph.AddActivities(new Activity("Make appraisal appointment") { Included = false, Roles = "Mobile consultant" });

            graph.AddActivities(new Activity("Appraisal audit") { Included = true, Roles = "Auditor" });

            graph.AddActivities(new Activity("On-site appraisal") { Included = true, Roles = "Mobile consulant" });

            graph.AddActivities(new Activity("Submit budget") { Included = true, Roles = "Customer" });

            graph.AddActivities(new Activity("Budget screening approve") { Included = true, Pending = true, Roles = "Intern" });
            
            graph.AddActivities(new Activity("Statistical appraisal") { Included = true, Roles = "Caseworker" });

            graph.AddActivities(new Activity("Assess loan application") { Included = true, Pending = true, Roles = "Caseworker" });

            graph.AddCondition("Collect Documents", "Irregular neighbourhood");

            graph.AddCondition("Collect Documents", "Assess loan application");

            graph.AddIncludeExclude(true, "Irregular neighbourhood", "Make appraisal appointment");

            graph.AddIncludeExclude(false, "Irregular neighbourhood", "Statistical appraisal");

            graph.AddCondition("Make appraisal appointment", "On-site appraisal");

            graph.AddIncludeExclude(true, "Appraisal audit", "On-site appraisal");

            graph.AddIncludeExclude(false, "Statistical appraisal", "On-site appraisal");
            graph.AddCondition("Statistical appraisal", "Assess loan application");

            graph.AddIncludeExclude(false,  "On-site appraisal", "Statistical appraisal");
            graph.AddCondition("On-site appraisal", "Assess loan application");
            graph.AddCondition("Budget screening approve", "Assess loan application");

            graph.AddResponse("Budget screening approve", "Assess loan application");

            graph.AddCondition("Submit budget", "Budget screening approve");
            graph.AddResponse("Submit budget", "Budget screening approve");
            
            LogGenerator9001 logGenerator9001 = new LogGenerator9001(20,graph);

            Log log = new Log();

            foreach (var trace in logGenerator9001.GenerateLog(500))
            {
                log.AddTrace(trace);
            }


           

            using (StreamWriter sw = new StreamWriter("C:/Downloads/mortgageLog.xml"))
            {
                sw.WriteLine(Log.ExportToXml(log));
            }
        }

        public void ParseMortgageApplication2()
        {
            var graph = new DcrGraph();

            graph.AddActivities(new Activity("Collect Documents", "Collect Documents") { Included = true, Roles = "Caseworker" });
            graph.AddIncludeExclude(false, "Collect Documents", "Collect Documents");

            graph.AddActivities(new Activity("Irregular neighbourhood", "Irregular neighbourhood") { Included = true, Roles = "it" });
            graph.AddIncludeExclude(false, "Irregular neighbourhood", "Irregular neighbourhood");

            graph.AddActivities(new Activity("Make appraisal appointment", "Make appraisal appointment") { Included = true, Roles = "Mobile consultant" });
            graph.AddIncludeExclude(false, "Make appraisal appointment", "Make appraisal appointment");

            graph.AddActivities(new Activity("Appraisal audit", "Appraisal audit") { Included = true, Roles = "Auditor" });
            graph.AddIncludeExclude(false, "Appraisal audit", "Appraisal audit");

            graph.AddActivities(new Activity("On-site appraisal", "On-site appraisal") { Included = true, Roles = "Mobile consulant" });
            graph.AddIncludeExclude(false, "On-site appraisal", "On-site appraisal");

            graph.AddActivities(new Activity("Submit budget", "Submit budget") { Included = true, Roles = "Customer" });
            graph.AddIncludeExclude(false, "Submit budget", "Submit budget");

            graph.AddActivities(new Activity("Budget screening approve", "Budget screening approve") { Included = true, Pending = true, Roles = "Intern" });
            graph.AddIncludeExclude(false, "Budget screening approve", "Budget screening approve");

            graph.AddActivities(new Activity("Statistical appraisal", "Statistical appraisal") { Included = true, Roles = "Caseworker" });
            graph.AddIncludeExclude(false, "Statistical appraisal", "Statistical appraisal");

            graph.AddActivities(new Activity("Assess loan application", "Assess loan application") { Included = true, Pending = true, Roles = "Caseworker" });
            graph.AddIncludeExclude(false, "Assess loan application", "Assess loan application");


            graph.AddCondition("Collect Documents", "Irregular neighbourhood");
            graph.AddCondition("Collect Documents", "Make appraisal appointment");
            graph.AddCondition("Collect Documents", "On-site appraisal");
            graph.AddCondition("Collect Documents", "Statistical appraisal");


            graph.AddIncludeExclude(false, "Statistical appraisal", "Irregular neighbourhood");
            graph.AddIncludeExclude(false, "Statistical appraisal", "Make appraisal appointment");
            graph.AddIncludeExclude(false, "Statistical appraisal", "On-site appraisal");

            graph.AddIncludeExclude(false, "Irregular neighbourhood","Statistical appraisal");
            graph.AddIncludeExclude(false, "Make appraisal appointment","Statistical appraisal" );
            graph.AddIncludeExclude(false, "On-site appraisal","Statistical appraisal");



            graph.AddCondition("Irregular neighbourhood", "Make appraisal appointment");
            graph.AddCondition("Make appraisal appointment", "On-site appraisal");
            graph.AddCondition("On-site appraisal", "Submit budget");
            graph.AddCondition("Submit budget", "Budget screening approve");
            graph.AddCondition("Budget screening approve", "Assess loan application");
            graph.AddCondition("Assess loan application","Appraisal audit");

            graph.AddCondition("Statistical appraisal", "Submit budget");

            var nested = new HashSet<Activity>();
            nested.Add(graph.GetActivity("Irregular neighbourhood"));
            nested.Add(graph.GetActivity("Make appraisal appointment"));
            nested.Add(graph.GetActivity("On-site appraisal"));

            graph.MakeNestedGraph(nested);


            //LogGenerator9001 logGenerator9001 = new LogGenerator9001(20, graph);

            //Log log = new Log();

            //foreach (var trace in logGenerator9001.GenerateLog(500))
            //{
            //    log.AddTrace(trace);
            //}




            using (StreamWriter sw = new StreamWriter("C:/Downloads/mortgageStrict.xml"))
            {
                sw.WriteLine(graph.ExportToXml());
            }
        }

        public static DcrGraph ParseDreyerLog()
        {
            var graph = new DcrGraph();

            graph.AddActivities(new Activity("Execute abandon", "Execute abandon") {Included = true, Roles = "Caseworker"});
            
            graph.AddActivities(new Activity("Change phase to abandon", "Change phase to abandon") { Included = true, Roles = "Nobody" });
            
            graph.AddActivities(new Activity("Round ends", "Round ends") {Included = true, Pending = false, Roles = "it"});

            graph.AddActivities(new Activity("Fill out application", "Fill out application") { Included = true, Pending = false, Roles = "Nobody" });

            graph.AddActivities(new Activity("End", "End") { Included = true, Pending = false, Roles = "*" });

            graph.AddActivities(new Activity("Screening approve", "Screening approve") { Included = true, Pending = false, Roles = "caseworker" });
            graph.AddActivities(new Activity("File architect", "File architect") { Included = true, Pending = false, Roles = "it" });
            graph.AddActivities(new Activity("Screening reject", "Screening reject") { Included = true, Pending = false, Roles = "caseworker" });
            graph.AddActivities(new Activity("File lawyer", "File lawyer") { Included = true, Pending = false, Roles = "it" });

            graph.AddActivities(new Activity("Review 1", "Review 1") { Included = true, Pending = false, Roles = "lawyer" });
            graph.AddActivities(new Activity("Review 2", "Review 2") { Included = true, Pending = false, Roles = "architect" });
            graph.AddActivities(new Activity("Review 3", "Review 3") { Included = true, Pending = false, Roles = "lawyer" });
            graph.AddActivities(new Activity("Review 4", "Review 4") { Included = true, Pending = false, Roles = "architect" });

            graph.AddActivities(new Activity("Round approved", "Round approved") { Included = true, Pending = false, Roles = "it" });
            
            graph.AddActivities(new Activity("Set pre-approved", "Set pre-approved") { Included = true, Pending = false, Roles = "it" });
            graph.AddActivities(new Activity("Inform approve", "Inform approve") { Included = true, Pending = false, Roles = "casework" });
            graph.AddActivities(new Activity("Receive end report", "Receive end report") { Included = true, Pending = false, Roles = "caseworker" });
            
            graph.AddActivities(new Activity("Payout", "Payout") { Included = true, Pending = false, Roles = "it" });
            graph.AddActivities(new Activity("Payout complete", "Payout complete") { Included = true, Pending = false, Roles = "it" });
            graph.AddActivities(new Activity("Undo payout", "Undo payout") { Included = true, Pending = false, Roles = "caseworker" });

            graph.AddActivities(new Activity("Reject application", "Reject application") { Included = true, Pending = false, Roles = "board" });
            graph.AddActivities(new Activity("Approve application", "Approve application") { Included = true, Pending = false, Roles = "board" });
            graph.AddActivities(new Activity("Note decision", "Note decision") { Included = true, Pending = false, Roles = "caseworker" });

            graph.AddActivities(new Activity("Account no changed", "Account no changed") { Included = false, Pending = false, Roles = "it" });

            graph.AddActivities(new Activity("Approve account no", "Approve account no") { Included = true, Pending = false, Roles = "accountant" });

            graph.AddActivities(new Activity("Guard", "Guard") { Included = true, Pending = true, Roles = "*" });
            //Should abort be included???
            graph.AddActivities(new Activity("Abort application", "Abort application") { Included = false, Pending = false, Roles = "caseworker" });
            graph.AddActivities(new Activity("Inform reject", "Inform reject") { Included = false, Pending = false, Roles = "caseworker"});
            graph.AddActivities(new Activity("Purge application", "Purge application") { Included = false, Pending = false, Roles = "it" });

            graph.AddIncludeExclude(false, "File architect", "File lawyer");
            graph.AddIncludeExclude(false, "File lawyer", "File architect");

            graph.AddCondition("Approve application", "Note decision");
            graph.AddCondition("Reject application", "Note decision");

            graph.AddIncludeExclude(false, "Payout", "Payout");
            graph.AddCondition("Payout", "Undo payout");

            graph.AddIncludeExclude(true, "Undo payout", "Payout");
            graph.AddResponse("Undo payout", "Payout");

            graph.AddResponse("Payout", "Payout complete");
            graph.AddIncludeExclude(false, "Payout complete", "Undo payout");
            graph.AddMileStone("Payout", "Payout complete");

            graph.AddCondition("Abort application", "Inform reject");
            graph.AddResponse("Abort application", "Inform reject");
            graph.AddCondition("Inform reject", "Purge application");
            graph.AddResponse("Inform reject", "Purge application");

            graph.AddIncludeExclude(true, "Screening reject", "Inform reject");
            graph.AddIncludeExclude(true, "Reject application", "Inform reject");

            graph.AddIncludeExclude(false, "Screening approve", "Screening reject");

            graph.AddCondition("Screening approve", "File lawyer");
            graph.AddCondition("Screening approve", "File architect");

            graph.AddCondition("File lawyer", "Review 1");
            graph.AddCondition("File lawyer", "Review 2");
            graph.AddCondition("File lawyer", "Review 3");
            graph.AddCondition("File lawyer", "Review 4");
            graph.AddCondition("File architect", "Review 1");
            graph.AddCondition("File architect", "Review 2");
            graph.AddCondition("File architect", "Review 3");
            graph.AddCondition("File architect", "Review 4");

            graph.AddIncludeExclude(false, "File architect", "Review 1");
            graph.AddIncludeExclude(false, "File lawyer", "Review 2");

            graph.AddCondition("Fill out application", "Screening approve");
            graph.AddCondition("Fill out application", "Screening reject");
            graph.AddResponse("Fill out application", "Payout");

            graph.AddCondition("Review 3", "Approve application");
            graph.AddCondition("Review 4", "Approve application");
            graph.AddCondition("Review 3", "Reject application");
            graph.AddCondition("Review 4", "Reject application");

            graph.AddCondition("Inform approve", "Payout");
            graph.AddCondition("Inform approve", "Receive end report");
            graph.AddResponse("Approve application", "Set pre-approved");

            graph.AddIncludeExclude(true, "Set pre-approved", "Abort application");

            graph.AddCondition("Payout", "Receive end report");
            graph.AddMileStone("Payout", "Receive end report");

            graph.AddIncludeExclude(false, "Round approved", "Set pre-approved");
            graph.AddResponse("Round approved", "Approve application");
            graph.AddResponse("Round approved", "Reject application");
            graph.AddResponse("Round approved", "Set pre-approved");

            graph.AddResponse("Account no changed", "Approve account no");
            graph.AddCondition("Account no changed", "Approve account no");
            graph.AddMileStone("Approve account no", "Payout");
            graph.AddIncludeExclude(false, "Payout", "Abort application");
            graph.AddIncludeExclude(false, "Payout", "Account no changed");

            graph.AddIncludeExclude(true, "Reject application", "Inform reject");

            graph.AddCondition("Approve application", "Set pre-approved");
            graph.AddCondition("Approve application", "Inform approve");

            graph.AddIncludeExclude(false, "Inform approve", "Review 1");
            graph.AddIncludeExclude(false, "Inform approve", "Review 2");
            graph.AddIncludeExclude(false, "Inform approve", "Review 3");
            graph.AddIncludeExclude(false, "Inform approve", "Review 4");
            graph.AddIncludeExclude(false, "Inform approve", "Approve application");
            graph.AddIncludeExclude(false, "Inform approve", "Reject application");
            graph.AddIncludeExclude(false, "Inform approve", "Note decision");
            graph.AddIncludeExclude(false, "Inform approve", "Abort application");

            graph.AddCondition("Inform approve", "Payout");
            graph.AddCondition("Inform approve", "Receive end report");

            graph.AddResponse("Approve application", "Set pre-approved");
            graph.AddCondition("Payout", "Receive end report");
            graph.AddMileStone("Payout", "Receive end report");

            graph.AddResponse("Approve application", "Payout");
            graph.AddIncludeExclude(true,"Approve application", "Approve account no");
            graph.AddIncludeExclude(true, "Set pre-approved", "Approve account no");

            graph.AddIncludeExclude(false, "Receive end report", "Guard");
            graph.AddIncludeExclude(false, "Inform reject", "Guard");
            graph.AddCondition("Guard","Guard");
            graph.AddCondition("Guard", "End");

            graph.AddResponse("Fill out application","End");

            return graph;
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
            var exAl = new ContradictionApproach(activities);

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
                        exAl.Graph = new RedundancyRemover().RemoveRedundancy(exAl.Graph);
                        break;
                    case "POST":
                        exAl.Graph = ContradictionApproach.PostProcessing(exAl.Graph);
                        break;
                    case "NESTED":
                        exAl.Graph = ContradictionApproach.CreateNests(exAl.Graph);
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
            dcrGraph.IncludeExcludes.Add(activityA, new Dictionary<Activity, Confidence> { { activityB, new Confidence() {Invocations = 1,Violations = 1} }, { activityC, new Confidence() } });
            dcrGraph.Conditions.Add(activityA, new Dictionary<Activity, Confidence> { { activityB, new Confidence()  }, { activityC, new Confidence() } });

            Console.WriteLine(dcrGraph);

            Console.WriteLine("--------------------------------------------------------------------------------");

            var copy = dcrGraph.Copy();
            Console.WriteLine(copy);
            Console.ReadKey();
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

        //    var unique = new UniqueTraceFinder(graph);

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

            Console.WriteLine(new RedundancyRemover().RemoveRedundancy(graph));

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

            Console.WriteLine(new RedundancyRemover().RemoveRedundancy(graph));

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

            Console.WriteLine(new RedundancyRemover().RemoveRedundancy(graph));

            Console.ReadLine();

            // Conclusion: Edits made to not see self-exclusion as redundant
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
                activities.Add(new Activity("" + ch, "" + ch));
            }

            var exAl = new ContradictionApproach(activities);

            

            var trace1 = new LogTrace {Id = "1"};
            trace1.AddEventsWithChars('A', 'B', 'E');
            var trace2 = new LogTrace { Id = "2" };
            trace2.AddEventsWithChars('A', 'C', 'F', 'A', 'B', 'B', 'F');
            var trace3 = new LogTrace { Id = "3" };
            trace3.AddEventsWithChars('A', 'C', 'E');
            var trace4 = new LogTrace { Id = "4" };
            trace4.AddEventsWithChars('A', 'D', 'F');
            var trace5 = new LogTrace { Id = "5" };
            trace5.AddEventsWithChars('A', 'B', 'F', 'A', 'B', 'E');
            var trace6 = new LogTrace { Id = "6" };
            trace6.AddEventsWithChars('A', 'C', 'F');
            var trace7 = new LogTrace { Id = "7" };
            trace7.AddEventsWithChars('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E');
            var trace8 = new LogTrace { Id = "8" };
            trace8.AddEventsWithChars('A', 'B', 'B', 'B', 'F');
            var trace9 = new LogTrace { Id = "9" };
            trace9.AddEventsWithChars('A', 'B', 'B', 'E');
            var trace10 = new LogTrace { Id = "10" };
            trace10.AddEventsWithChars('A', 'C', 'F', 'A', 'C', 'E');

            Log log = new Log() {Traces = {trace1, trace2, trace3, trace4, trace5, trace6, trace7, trace8, trace9, trace10}};
            
            exAl.AddLog(log);

            Console.WriteLine(exAl.Graph);
            Console.WriteLine(QualityDimensionRetriever.Retrieve(exAl.Graph,log));
            //Console.WriteLine(exAl.Graph.ExportToXml());
            Console.ReadLine();

            exAl.Graph = new RedundancyRemover().RemoveRedundancy(exAl.Graph);

            Console.WriteLine(exAl.Graph);
            Console.WriteLine(QualityDimensionRetriever.Retrieve(exAl.Graph, log));
            Console.ReadLine();

            exAl.Graph = ContradictionApproach.PostProcessing(exAl.Graph);


            Console.WriteLine(exAl.Graph);
            Console.WriteLine(QualityDimensionRetriever.Retrieve(exAl.Graph, log));

            Console.ReadLine();
            //using (StreamWriter sw = new StreamWriter("C:/Downloads/OrigGraph.xml"))
            //{
            //    sw.WriteLine(exAl.Graph.ExportToXml());
            //}
        }
        
        public void GetQualityMeasuresOnStatisticsOriginalGraph()
        {
            var trace1 = new LogTrace { Id = "1" };
            trace1.AddEventsWithChars('A', 'B', 'E');
            var trace2 = new LogTrace { Id = "2" };
            trace2.AddEventsWithChars('A', 'C', 'F', 'A', 'B', 'B', 'F');
            var trace3 = new LogTrace { Id = "3" };
            trace3.AddEventsWithChars('A', 'C', 'E');
            var trace4 = new LogTrace { Id = "4" };
            trace4.AddEventsWithChars('A', 'D', 'F');
            var trace5 = new LogTrace { Id = "5" };
            trace5.AddEventsWithChars('A', 'B', 'F', 'A', 'B', 'E');
            var trace6 = new LogTrace { Id = "6" };
            trace6.AddEventsWithChars('A', 'C', 'F');
            var trace7 = new LogTrace { Id = "7" };
            trace7.AddEventsWithChars('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E');
            var trace8 = new LogTrace { Id = "8" };
            trace8.AddEventsWithChars('A', 'B', 'B', 'B', 'F');
            var trace9 = new LogTrace { Id = "9" };
            trace9.AddEventsWithChars('A', 'B', 'B', 'E');
            var trace10 = new LogTrace { Id = "10" };
            trace10.AddEventsWithChars('A', 'C', 'F', 'A', 'C', 'E');

            Log log = new Log() { Traces = { trace1, trace2, trace3, trace4, trace5, trace6, trace7, trace8, trace9, trace10 } };

            var graph = new DcrGraph();
            for (char ch = 'A'; ch <= 'F'; ch++)
            {
                if (ch == 'D') continue;
                graph.AddActivity(ch.ToString(), "somename" + ch);
            }
            graph.SetIncluded(true, "A");

            // Self-excludes
            graph.AddIncludeExclude(false, "A", "A");
            graph.AddIncludeExclude(false, "C", "C");
            graph.AddIncludeExclude(false, "E", "E");
            graph.AddIncludeExclude(false, "F", "F");

            // Includes
            graph.AddIncludeExclude(true, "A", "B");
            graph.AddIncludeExclude(true, "A", "C");
            graph.AddIncludeExclude(true, "B", "E");
            graph.AddIncludeExclude(true, "B", "F");
            graph.AddIncludeExclude(true, "C", "E");
            graph.AddIncludeExclude(true, "C", "F");
            graph.AddIncludeExclude(true, "F", "A");

            // Excludes
            graph.AddIncludeExclude(false, "E", "B");
            graph.AddIncludeExclude(false, "B", "C");
            graph.AddIncludeExclude(false, "C", "B");
            graph.AddIncludeExclude(false, "F", "B");
            graph.AddIncludeExclude(false, "E", "F");
            graph.AddIncludeExclude(false, "F", "E");

            //var redundRemoved = new RedundancyRemover().RemoveRedundancy(graph);
            
            Console.WriteLine(QualityDimensionRetriever.Retrieve(graph, log));
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

            var graph2 = new RedundancyRemover().RemoveRedundancy(graph);

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

            var graph2 = new RedundancyRemover().RemoveRedundancy(graph);
            

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

            var exAl = new ContradictionApproach(activities);

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
            exAl.Graph = new RedundancyRemover().RemoveRedundancy(exAl.Graph);

            res = QualityDimensionRetriever.Retrieve(exAl.Graph, log);
            Console.WriteLine(exAl.Graph);
            Console.WriteLine(res);

            Console.ReadLine();
        }

        [STAThread]
        public void AprioriLogAndGraphQualityMeasureRun()
        {
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

            var dialog = new OpenFileDialog();
            dialog.Title = "Select a graph XML-file";
            dialog.Filter = "XML files (*.xml)|*.xml";

            DcrGraph graphFromXml = null;
            if (dialog.ShowDialog() == DialogResult.OK) // They selected a file
            {
                var filePath = dialog.FileName;
                var xml = File.ReadAllText(filePath);

                try
                {
                    graphFromXml = XmlParser.ParseDcrGraph(xml); // Throws exception if failure
                }
                catch
                {
                    MessageBox.Show("Could not parse DCR-graph.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            Console.WriteLine(QualityDimensionRetriever.Retrieve(graphFromXml, new Log { Traces = originalLog }));

            Console.ReadLine();
        }

        public void BigDataTest()
        {
            for (int i = 1; i <= 10000000; i*=10)
            {
                var t = 10;
                //var tl = 10;
                var x = 10;
                var al = 20;
                var rs = DataTimingTest(al, t, i, x);

                Console.WriteLine(@"Traces: {0}, Alphabet: {1} TraceLenght: {2}, timesrun: {3}, RESULT: {4},MIN:{5},MAX:{6},Std.Dev:{7}",t,al,i,x,rs.Average(),rs.Min(),rs.Max(),StdDev(rs));
            }

            Console.ReadLine();

        }


        public List<long> DataTimingTest(int alphabeth, int traces, int traceLength, int timesToRun)
        {
            var activities = new HashSet<Activity>();

            for (int i = 0; i < alphabeth; i++)
            {
                activities.Add(new Activity("" + i, "" + i));
            }

            var times = new List<long>();

            for (int j = 0; j < timesToRun; j++)
            {
                
                var rnd = new Random();

                var inputLog = new Log();
                var traceId = 1000;
                var currentTrace = new LogTrace() {Id = traceId.ToString()};
                while (inputLog.Traces.Count < traces)
                {
                    currentTrace.Add(new LogEvent(activities.ElementAt(rnd.Next(activities.Count)).Id, ""));

                    if (currentTrace.Events.Count == traceLength)
                    {
                        inputLog.AddTrace(currentTrace);
                        traceId++;
                        currentTrace = (new LogTrace() {Id = traceId.ToString()});

                    }

                }
                
                //
                var watch = new Stopwatch();
                watch.Start();

                var exAl = new ContradictionApproach(activities);

                foreach (var trace in inputLog.Traces)
                {
                    exAl.AddTrace(trace);
                }

                watch.Stop();

                times.Add(watch.ElapsedMilliseconds);
                Console.Write(".");
            }

            return times;
        }


        private static double StdDev(IEnumerable<long> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }

        public void OriginalLogStatisticsGraphMeasures()
        {
            var originalLog = new List<LogTrace>
            {
                new LogTrace('A', 'B', 'E'),
                new LogTrace('A', 'C', 'F', 'A', 'B', 'B', 'F'),
                new LogTrace('A', 'C', 'E'),
                new LogTrace('A', 'D', 'F'),
                new LogTrace('A', 'B', 'F', 'A', 'B', 'E'),
                new LogTrace('A', 'C', 'F'),
                new LogTrace('A', 'B', 'F', 'A', 'C', 'F', 'A', 'C', 'E'),
                new LogTrace('A', 'B', 'B', 'B', 'F'),
                new LogTrace('A', 'B', 'B', 'E'),
                new LogTrace('A', 'C', 'F', 'A', 'C', 'E')
            };

            var apriori = new DcrGraph();
            apriori.AddActivities(new Activity("A", "nameA"), new Activity("B", "nameB"), new Activity("C", "nameC"),
                new Activity("E", "nameE"), new Activity("F", "nameF"));
            apriori.SetIncluded(true, "A");
            apriori.SetIncluded(false, "B");
            apriori.SetIncluded(false, "C");
            apriori.SetIncluded(false, "E");
            apriori.SetIncluded(false, "F");

            apriori.AddResponse("A", "B");
            apriori.AddResponse("A", "C");
            apriori.AddResponse("A", "E");
            apriori.AddResponse("A", "F");
            apriori.AddResponse("B", "E");

            // self-excludes
            apriori.AddIncludeExclude(false, "A", "A");
            apriori.AddIncludeExclude(false, "C", "C");
            apriori.AddIncludeExclude(false, "E", "E");
            apriori.AddIncludeExclude(false, "F", "F");

            apriori.AddIncludeExclude(true, "A", "B");
            apriori.AddIncludeExclude(true, "A", "C");
            apriori.AddIncludeExclude(true, "F", "A");
            apriori.AddIncludeExclude(true, "B", "E");
            apriori.AddIncludeExclude(true, "B", "F");
            apriori.AddIncludeExclude(true, "C", "E");
            apriori.AddIncludeExclude(true, "C", "F");

            apriori.AddIncludeExclude(false, "B", "C");
            apriori.AddIncludeExclude(false, "C", "B");
            apriori.AddIncludeExclude(false, "E", "B");
            apriori.AddIncludeExclude(false, "E", "F");
            apriori.AddIncludeExclude(false, "F", "E");
            apriori.AddIncludeExclude(false, "F", "B");

            Console.WriteLine(QualityDimensionRetriever.Retrieve(apriori, new Log { Traces = originalLog }));

            Console.ReadLine();
        }

        public void SomeTestForFindingTracesBeforeAfterStuff()
        {
            var dcr = new DcrGraph();
            dcr.AddActivities(new Activity("A", "A"), new Activity("B", "B"), new Activity("C", "C"));
            dcr.AddCondition("A", "B");
            dcr.AddCondition("A", "C");
            dcr.AddCondition("B", "C");
            dcr.SetIncluded(true, "A");
            dcr.SetIncluded(true, "B");
            dcr.SetIncluded(true, "C");
            dcr.SetPending(true, "C");

            var traceFinder = new UniqueTraceFinder(new ByteDcrGraph(dcr));

            dcr = dcr.Copy();
            dcr.RemoveCondition("A", "B");
            traceFinder = new UniqueTraceFinder(new ByteDcrGraph(dcr));
        }
    }
}
