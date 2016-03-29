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
    public class XmlParser
    {
        private DcrGraph _resultGraph;

        public Log ParseLog(string xml)
        {
            // TODO: Find log standard form to use/follow - need case log
            return new Log();
        }

        private void ParseConditions(XDocument doc)
        {
            foreach (var condition in doc.Descendants("conditions").Elements())
            {
                _resultGraph.AddCondition(condition.Attribute("sourceId").Value, condition.Attribute("targetId").Value);
            }
        }

        //private List<Constraint> ParseResponses(XDocument doc)
        //{
        //    var ResponseList = new List<Constraint>();

        //    foreach (var response in doc.Descendants("responses").Elements())
        //    {
        //        ResponseList.Add(new Constraint()
        //        {
        //            fromNodeId = response.Attribute("sourceId").Value,
        //            toNodeId = response.Attribute("targetId").Value
        //        });
        //    }

        //    return ResponseList;
        //}

        //private List<Constraint> ParseExclusions(XDocument doc)
        //{
        //    var ExcludesList = new List<Constraint>();
        //    foreach (var exclude in doc.Descendants("excludes").Elements())
        //    {
        //        ExcludesList.Add(new Constraint()
        //        {
        //            fromNodeId = exclude.Attribute("sourceId").Value,
        //            toNodeId = exclude.Attribute("targetId").Value
        //        });
        //    }
        //    return ExcludesList;
        //}

        //private List<Constraint> ParseIncludes(XDocument doc)
        //{
        //    var IncludesList = new List<Constraint>();
        //    foreach (var include in doc.Descendants("includes").Elements())
        //    {
        //        IncludesList.Add(new Constraint()
        //        {
        //            fromNodeId = include.Attribute("sourceId").Value,
        //            toNodeId = include.Attribute("targetId").Value
        //        });
        //    }
        //    return IncludesList;
        //}

        //private List<Constraint> ParseMilestones(XDocument doc)
        //{
        //    var MilestonesList = new List<Constraint>();
        //    foreach (var milestone in doc.Descendants("milestones").Elements())
        //    {
        //        MilestonesList.Add(new Constraint()
        //        {
        //            fromNodeId = milestone.Attribute("sourceId").Value,
        //            toNodeId = milestone.Attribute("targetId").Value
        //        });
        //    }
        //    return MilestonesList;
        //}

        //private List<Constraint> ParseConditionsReversed(XDocument doc)
        //{
        //    var ConditionList = new List<Constraint>();

        //    foreach (var condition in doc.Descendants("conditions").Elements())
        //    {
        //        ConditionList.Add(new Constraint()
        //        {
        //            fromNodeId = condition.Attribute("targetId").Value,
        //            toNodeId = condition.Attribute("sourceId").Value
        //        });
        //    }

        //    return ConditionList;
        //}

        //private List<Constraint> ParseResponsesReversed(XDocument doc)
        //{
        //    var ResponseList = new List<Constraint>();

        //    foreach (var response in doc.Descendants("responses").Elements())
        //    {
        //        ResponseList.Add(new Constraint()
        //        {
        //            fromNodeId = response.Attribute("targetId").Value,
        //            toNodeId = response.Attribute("sourceId").Value
        //        });
        //    }

        //    return ResponseList;
        //}

        //private List<Constraint> ParseExclusionsReversed(XDocument doc)
        //{
        //    var ExcludesList = new List<Constraint>();
        //    foreach (var exclude in doc.Descendants("excludes").Elements())
        //    {
        //        ExcludesList.Add(new Constraint()
        //        {
        //            fromNodeId = exclude.Attribute("targetId").Value,
        //            toNodeId = exclude.Attribute("sourceId").Value
        //        });
        //    }
        //    return ExcludesList;
        //}

        //private List<Constraint> ParseIncludesReversed(XDocument doc)
        //{
        //    var IncludesList = new List<Constraint>();
        //    foreach (var include in doc.Descendants("includes").Elements())
        //    {
        //        IncludesList.Add(new Constraint()
        //        {
        //            fromNodeId = include.Attribute("targetId").Value,
        //            toNodeId = include.Attribute("sourceId").Value
        //        });
        //    }
        //    return IncludesList;
        //}

        //private List<Constraint> ParseMilestonesReversed(XDocument doc)
        //{
        //    var MilestonesList = new List<Constraint>();
        //    foreach (var milestone in doc.Descendants("milestones").Elements())
        //    {
        //        MilestonesList.Add(new Constraint()
        //        {
        //            fromNodeId = milestone.Attribute("targetId").Value,
        //            toNodeId = milestone.Attribute("sourceId").Value
        //        });
        //    }
        //    return MilestonesList;
        //}

        //private List<string> ParseRoles(XDocument doc)
        //{
        //    var RolesList = new List<string>();

        //    foreach (var role in doc.Descendants("roles").Elements().Where(element => element.Parent.Parent.Parent.Name != "event"))
        //    {
        //        if (role.Value != "") RolesList.Add(role.Value);
        //    }

        //    return RolesList;
        //}
    }
}
