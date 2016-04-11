using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Parsing
{
    /// <summary>
    /// Inspiration drawn from kulr@itu.dk's 2nd year project at ITU, semester 4 - no direct code duplication, however.
    /// </summary>
    public static class XmlParser
    {
        #region Log parsing (jf. BPI Challenge 2015 format)

        public static Log ParseHospitalLog(string xml)
        {
            XDocument doc = XDocument.Parse(xml);

            var log = new Log();

            ParseHospitalTracesAndBuildAlphabet(log, doc);

            return log;
        }

        public static Log ParseUlrikHovsgaardLog(string xml)
        {
            XDocument doc = XDocument.Parse(xml);

            var log = new Log();

            ParseUlrikHovsgaardTracesAndBuildAlphabet(log, doc);

            return log;
        }

        public static Log ParseLog(string xml)
        {
            //TODO: take the mappings of the names of relevant fields as input.
            
            Console.WriteLine("String length: " + xml.Length);
            
            //slight hack TODO: just alter the xml file like this instead.
            xml = Regex.Replace(xml, "concept:name", "conceptName");


            var log = new Log();

            XNamespace ns = "http://www.xes-standard.org/";


            XDocument doc = XDocument.Parse(xml);

            foreach (XElement traceElement in doc.Root.Elements(ns + "trace"))
            {
                var trace = new LogTrace() {Id = traceElement.GetValue(ns,"conceptName") };

                foreach (XElement eventElement in traceElement.Elements(ns + "event"))
                {
                    trace.Add(new LogEvent(eventElement.GetValue(ns,"conceptName"),
                                            eventElement.GetValue(ns, "activityNameEN")));
                }


                log.AddTrace(trace);
            }

            return log;

            #region oldAttempts

            /*  TRYING WITH XMLDOCUMENT
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            var logNode = doc.SelectSingleNode("/log");

            var traces = logNode.SelectNodes("trace");

            foreach (XmlNode xmlTrace in traces)
            {
                List<LogEvent> events = new List<LogEvent>();

                foreach (XmlNode eventNode in xmlTrace.SelectNodes("event"))
                {
                    events.Add(
                        new LogEvent(eventNode.SelectSingleNode("concept:name").InnerText,
                            eventNode.SelectSingleNode("activityNameNL").InnerText)
                        );
                }

                log.AddTrace(
                    new LogTrace()
                    {
                        Id = xmlTrace.SelectSingleNode("concept:name").InnerText,
                        IsFinished = true,
                        Events = events
                    });
            }*/

            /*
            List<LogTrace> traces = (from t in doc.Descendants(ns + "trace")
                select new LogTrace()
                {
                    //Id = t.Element(ns + "concept:name")?.Value,
                    IsFinished = true,
                    Events = (from e in t.Descendants(ns + "event")
                              select new LogEvent(e.Element(ns + "activityNameNL").Value,e.Element(ns +"activityNameNL").Value)).ToList<LogEvent>()
                }).ToList<LogTrace>();*/



            //ParseTracesAndBuildAlphabet(log, doc);
            #endregion
        }

        #region Log parsing privates

        private static string GetValue(this XElement element,XNamespace ns, string attribute) => (string)element.Descendants(ns + "string").First(x => x.Attribute("key").Value == attribute).Attribute("value");
        

        // TODO: doesn't work, dunno how to find log title
        private static string ParseLogTitle(XDocument doc)
        {
            throw new NotImplementedException();
            //var firstAttribute = doc.Descendants("dcrgraph").First().FirstAttribute;
            //if (firstAttribute != null)
            //{
            //    return firstAttribute.Value;
            //}
            //return null;
        }

        private static void ParseUlrikHovsgaardTracesAndBuildAlphabet(Log log, XDocument doc)
        {
            log.Id = doc.Descendants("log").First().Attribute("id").Value;

            IEnumerable<XElement> traces = doc.Descendants("trace");
            foreach (var traceElement in traces)
            {
                var currentTrace = new LogTrace();

                currentTrace.Id = traceElement.Attribute("id").Value;

                IEnumerable<XElement> events = traceElement.Descendants("event");
                foreach (var eve in events)
                {
                    var id = eve.Attribute("id").Value;
                    var name = eve.Attribute("name").Value;

                    var logEvent = new LogEvent(id, name);

                    currentTrace.Add(logEvent);
                }

                log.AddTrace(currentTrace);
            }
        }

        // TODO: finish it!
        private static void ParseHospitalTracesAndBuildAlphabet(Log log, XDocument doc)
        {
            XNamespace ns = "http://www.xes-standard.org/";

            IEnumerable<XElement> traces = doc.Descendants(ns + "trace");

            foreach (var traceElement in traces)
            {
                var currentTrace = new LogTrace();
                
                //it will not find "concept:name" as colon is illegal char. Concept is a namespace. Maybe not .Element
                //currentTrace.Id = traceElement.Element((concept + "name")).Value; // Integer value

                IEnumerable<XElement> events = traceElement.Descendants(ns + "event");

                foreach (var eve in events)
                {
                    //var pairs = XDocument.Parse(eve.Value)
                    //    .Descendants("string")
                    //    .Select(x => new
                    //    {
                    //        Key = x.Attribute("key").Value,
                    //        Value = x.Attribute("value").Value
                    //    })
                    //    .ToDictionary(item => item.Key, item => item.Value);

                    // Retrieve Id
                    
                    var id2 =
                        eve.Descendants(ns + "string")
                            .Where(x => x.Attribute("key").Value == "Activity code")
                            .Select(y => y.Attribute("value").Value).ToList().FirstOrDefault();
                    var id = eve.Element(ns + "Activity code").Attribute("value").Value;

                    // Assigning Name:
                    var name = eve.Element(ns + "activityNameEN").Attribute("value").Value;

                    var logEvent = new LogEvent(id, name);

                    log.Alphabet.Add(logEvent); // Adding to HashSet, no problem

                    currentTrace.Add(logEvent.Copy());
                }

                log.Traces.Add(currentTrace);
            }
        }

        private static void ParseTracesAndBuildAlphabet(Log log, XDocument doc)
        {
            XNamespace ns = "http://www.xes-standard.org/";
            XNamespace concept = "{http://www.xes-standard.org/concept.xesext}";

            IEnumerable<XElement> traces = doc.Descendants(ns + "trace");

            foreach (var traceElement in traces)
            {
                var currentTrace = new LogTrace();


                //it will not find "concept:name" as colon is illegal char. Concept is a namespace. Maybe not .Element
                //currentTrace.Id = traceElement.Element((concept + "name")).Value; // Integer value

                IEnumerable<XElement> events = traceElement.Descendants(ns + "event").Where(element => element.HasElements);

                foreach (var eve in events)
                {
                    // Retrieve Id
                    var id = eve.Element(ns + "action_code").Attribute("value").Value;

                    // Assigning Name:
                    var name = eve.Element(ns + "activityNameEN").Attribute("value").Value;

                    var logEvent = new LogEvent(id, name);

                    log.Alphabet.Add(logEvent); // Adding to HashSet, no problem

                    currentTrace.Add(logEvent.Copy());
                }

                log.Traces.Add(currentTrace);
            }
        }

        #endregion

        #endregion

        #region DCR-graph parsing

        public static DcrGraph ParseDcrGraph(string xml)
        {
            XDocument doc = XDocument.Parse(xml);


            //TODO: parse nested correctly
            var graph = new DcrGraph { Title = ParseGraphTitle(doc) };
            
            ParseActivities(graph, doc);

            ParseRelations(graph, doc);

            return graph;
        }

        #region DcrGraph Parsing privates

        private static string ParseGraphTitle(XDocument doc)
        {
            var firstAttribute = doc.Descendants("dcrgraph").First().FirstAttribute;

            //TODO: check for actual title-attribute
            if (firstAttribute != null)
            {
                return firstAttribute.Value;
            }
            return null;
        }

        private static void ParseActivities(DcrGraph graph, XDocument doc)
        {
            // Parsing initial states:
            var idOfIncludedEvents = (from includedEvent in doc.Descendants("included").Elements()
                                      select includedEvent.FirstAttribute.Value).ToList(); //Think about checking for ID.

            var idOfPendingEvents = (from pendingEvent in doc.Descendants("pendingResponses").Elements()
                                     select pendingEvent.FirstAttribute.Value).ToList(); //Think about checking for ID.

            var idOfExecutedEvents = (from executedEvent in doc.Descendants("executed").Elements()
                                      select executedEvent.FirstAttribute.Value).ToList(); //Think about checking for ID.

            IEnumerable<XElement> events = doc.Descendants("event");//.Where(element => element.HasElements); //Only takes event elements in events!

            foreach (var eve in events)
            {
                // Retrieve Id
                var id = eve.Attribute("id").Value;

                // Assigning Name:
                var name = (from labelMapping in doc.Descendants("labelMapping")
                            where labelMapping.Attribute("eventId").Value.Equals(id)
                            select labelMapping.Attribute("labelId").Value).FirstOrDefault();

                // Add activity to graph
                graph.AddActivity(id, name);

                // Assigning Roles:
                var roles = eve.Descendants("role");
                var rolesList = new List<string>();
                foreach (var role in roles)
                {
                    if (role.Value != "") rolesList.Add(role.Value);
                }
                graph.AddRolesToActivity(id, rolesList);

                // Mark Included
                if (idOfIncludedEvents.Contains(id)) graph.SetIncluded(true, id);

                // Mark Pending:
                if (idOfPendingEvents.Contains(id)) graph.SetPending(true, id);

                // Mark Executed:
                if (idOfExecutedEvents.Contains(id)) graph.SetExecuted(true, id);
            }
        }

        private static void ParseRelations(DcrGraph graph, XDocument doc)
        {
            foreach (var condition in doc.Descendants("conditions").Elements())
            {
                graph.AddCondition(condition.Attribute("sourceId").Value, condition.Attribute("targetId").Value);
            }
            foreach (var condition in doc.Descendants("responses").Elements())
            {
                graph.AddResponse(condition.Attribute("sourceId").Value, condition.Attribute("targetId").Value);
            }
            foreach (var condition in doc.Descendants("excludes").Elements())
            {
                graph.AddIncludeExclude(false, condition.Attribute("sourceId").Value, condition.Attribute("targetId").Value);
            }
            foreach (var condition in doc.Descendants("includes").Elements())
            {
                graph.AddIncludeExclude(true, condition.Attribute("sourceId").Value, condition.Attribute("targetId").Value);
            }
            foreach (var condition in doc.Descendants("milestones").Elements())
            {
                graph.AddMileStone(condition.Attribute("sourceId").Value, condition.Attribute("targetId").Value);
            }
        }

        #endregion

        #endregion
    }
}
