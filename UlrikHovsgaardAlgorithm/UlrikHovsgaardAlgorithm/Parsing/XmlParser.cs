using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Parsing
{
    /// <summary>
    /// Inspiration drawn from kulr@itu.dk's 2nd year project at ITU, semester 4 - no direct code duplication, however.
    /// </summary>
    public static class XmlParser
    {
        public static Log ParseLog(string xml)
        {
            // TODO: Find log standard form to use/follow - need case log
            throw new NotImplementedException();
        }

        public static DcrGraph ParseDcrGraph(string xml)
        {
            XDocument doc = XDocument.Parse(xml);

            var graph = new DcrGraph { Title = ParseGraphTitle(doc) };
            
            ParseActivities(graph, doc);

            ParseRelations(graph, doc);

            return graph;
        }

        #region DcrGraph Parsing privates

        private static string ParseGraphTitle(XDocument doc)
        {
            var firstAttribute = doc.Descendants("dcrgraph").First().FirstAttribute;
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

            IEnumerable<XElement> events = doc.Descendants("event").Where(element => element.HasElements); //Only takes event elements in events!

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
    }
}
