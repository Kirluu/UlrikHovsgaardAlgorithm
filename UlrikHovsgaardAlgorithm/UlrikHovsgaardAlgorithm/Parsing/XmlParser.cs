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
        #region Log parsing
        
        public static Log ParseLog(LogStandard logStandard, string xml)
        {
            //TODO: take the mappings of the names of relevant fields as input.
            
            //slight hack TODO: just alter the xml file like this instead.
            //xml = Regex.Replace(xml, "concept:name", "conceptName"); // TODO: remove
            
            var log = new Log();

            XNamespace ns = logStandard.Namespace;

            XDocument doc = XDocument.Parse(xml);

            int eventId = 0;
            foreach (XElement traceElement in doc.Root.Elements(ns + logStandard.TraceIdentifier).Where(element => element.HasElements))
            {
                var trace = new LogTrace() {Id = traceElement.GetValue(ns, logStandard.TraceIdIdentifier) };

                foreach (XElement eventElement in traceElement.Elements(ns + logStandard.EventIdentifier).Where(element => element.HasElements))
                {
                    trace.Add(new LogEvent(eventElement.GetValue(ns, logStandard.EventIdIdentifier),
                                            eventElement.GetValue(ns, logStandard.EventNameIdentifier))
                    {
                        EventId = eventId++.ToString(),
                        ActorName = eventElement.GetValue(ns, logStandard.ActorNameIdentifier)
                    });
                }
                
                log.AddTrace(trace);
            }
            //var names = new HashSet<string>();
            //var ids = new HashSet<string>();
            //foreach (var logEvent in log.Alphabet)
            //{
            //    ids.Add(logEvent.IdOfActivity);
            //    names.Add(logEvent.Name);
            //}

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
                        new LogEvent(eventNode.SelectSingleNode(conceptName).InnerText,
                            eventNode.SelectSingleNode("activityNameNL").InnerText)
                        );
                }

                log.AddTrace(
                    new LogTrace()
                    {
                        Id = xmlTrace.SelectSingleNode(conceptName).InnerText,
                        IsFinished = true,
                        Events = events
                    });
            }*/

            /*
            List<LogTrace> traces = (from t in doc.Descendants(ns + "trace")
                select new LogTrace()
                {
                    //Id = t.Element(ns + conceptName)?.Value,
                    IsFinished = true,
                    Events = (from e in t.Descendants(ns + "event")
                              select new LogEvent(e.Element(ns + "activityNameNL").Value,e.Element(ns +"activityNameNL").Value)).ToList<LogEvent>()
                }).ToList<LogTrace>();*/



            //ParseTracesAndBuildAlphabet(log, doc);
            #endregion
        }

        #region Log parsing privates


        private static string GetValue(this XElement element, XNamespace ns, LogStandardEntry attribute)
        {
            try
            {
                return (string)
                element.Descendants(ns + attribute.DataType.ToString().ToLower())
                    .First(x => x.Attribute("key").Value == attribute.Name)
                    .Attribute("value");
            }
            catch 
            {
                return "";
            }
            
        }

        #endregion

        #endregion

        #region DCR-graph parsing

        public static DcrGraph ParseDcrGraph(string xml)
        {
            XDocument doc = XDocument.Parse(xml);

            
            var graph = new DcrGraph {Title = ParseGraphTitle(doc)};

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
            IEnumerable<XElement> events = doc.Descendants("event").Where(element => element.HasElements); //Only takes event elements in events!

            foreach (var eve in events)
            {
                ParseActivity(graph, doc, eve);
            }
        }

        private static Activity ParseActivity(DcrGraph graph, XDocument doc, XElement eve)
        {
            var nestedEvents = eve.Descendants("event").Where(element => element.HasElements).ToList(); //Only takes event elements in events!

            bool isNestedGraph = nestedEvents.Count > 0;

            // Retrieve Id
            var id = eve.Attribute("id").Value;

            // Retrieve Name:
            var name = (from labelMapping in doc.Descendants("labelMapping")
                        where labelMapping.Attribute("eventId").Value.Equals(id)
                        select labelMapping.Attribute("labelId").Value).FirstOrDefault();

            // Check to see if Activity was already parsed
            if (graph.GetActivities().ToList().Exists(x => x.Id == id && x.Name == name)) return null;

            Activity activityToReturn;

            if (isNestedGraph)
            {
                var nestedActivities = new HashSet<Activity>();
                foreach (var nestedEvent in nestedEvents)
                {
                    nestedActivities.Add(ParseActivity(graph, doc, nestedEvent));
                }

                activityToReturn = graph.MakeNestedGraph(id, name, nestedActivities);
            }
            else // Not a nested graph --> Treat as single activity
            {
                // Add activity to graph
                activityToReturn = graph.AddActivity(id, name);

                // Assigning Roles:
                var roles = eve.Descendants("role");
                var rolesList = new List<string>();
                foreach (var role in roles)
                {
                    if (role.Value != "") graph.AddRolesToActivity(id,role.Value);
                }
                
                // Mark Included
                if ((from includedEvent in doc.Descendants("included").Elements()
                     select includedEvent.FirstAttribute.Value).Contains(id)) graph.SetIncluded(true, id);

                // Mark Pending:
                if ((from pendingEvent in doc.Descendants("pendingResponses").Elements()
                     select pendingEvent.FirstAttribute.Value).Contains(id)) graph.SetPending(true, id);

                // Mark Executed:
                if ((from executedEvent in doc.Descendants("executed").Elements()
                     select executedEvent.FirstAttribute.Value).Contains(id)) graph.SetExecuted(true, id);
            }

            return activityToReturn;
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
