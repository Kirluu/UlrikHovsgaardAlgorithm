using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Parsing
{
    public class XmlParser
    {
        public List<LogTrace> ParseLog(string xml)
        {
            // TODO: Find log standard form to use/follow - need case log
            return new List<LogTrace>();
        }

        public string ParseDcrGraphToXml(DcrGraph graph)
        {
            var xml = "<dcrgraph>\n";
            
            xml += "<specification>\n<resources>\n<events>\n"; // Begin events
            // Event definitions
            foreach (var activity in graph.Activities)
            {
                xml += string.Format(@"<event id=""{0}"" scope=""private"" >
    <custom>
        <visualization>
            <location xLoc = ""806"" yLoc=""183"" />
        </visualization>
        <roles>
            <role></role>
        </roles>
        <groups>
            <group />
        </groups>
        <eventType></eventType>
        <eventDescription></eventDescription>
        <level>1</level>
        <eventData></eventData>
    </custom>
</event>", activity.Id); // Consider removing location ? What will happen?
                xml += "\n";
            }

            xml += "</events>\n"; // End events
            xml += "<subProcesses></subProcesses>\n";
            xml += @"<distribution>
    <externalEvents></externalEvents>
</distribution>";
            xml += "\n";
            // Labels
            xml += "<labels>\n";
            foreach (var activity in graph.Activities)
            {
                xml += string.Format(@"<label id =""{0}""/>", activity.Name);
                xml += "\n";
            }
            xml += "</labels>\n";
            // Label mappings
            xml += "<labelMappings>\n";
            foreach (var activity in graph.Activities)
            {
                xml += string.Format(@"<labelMapping eventId =""{0}"" labelId = ""{1}""/>", activity.Id, activity.Name);
                xml += "\n";
            }
            xml += "</labelMappings>\n";
            // Stuff
            xml += @"<expressions></expressions>
    <variables></variables>
    <variableAccesses>
        <writeAccesses />
    </variableAccesses>
    <custom>
        <roles></roles>
        <groups></groups>
        <eventTypes></eventTypes>
        <graphDetails></graphDetails>
        <graphFilters>
            <filteredGroups></filteredGroups>
            <filteredRoles></filteredRoles>
        </graphFilters>
    </custom>
</resources>";
            xml += "\n";

            // Constraints
            xml += "<constraints>\n";
            // Conditions
            xml += "<conditions>\n";
            foreach (var condition in graph.Conditions)
            {
                foreach (var target in condition.Value)
                {
                    xml += string.Format(@"<exclude sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", condition.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</conditions>\n";

            // Responses
            xml += "<responses>\n";
            foreach (var response in graph.Responses)
            {
                foreach (var target in response.Value)
                {
                    xml += string.Format(@"<response sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", response.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</responses>\n";

            // Excludes
            xml += "<excludes>\n";
            foreach (var exclusion in graph.IncludeExcludes)
            {
                foreach (var target in exclusion.Value)
                {
                    if (!target.Value) // If it is an exclusion
                    {
                        xml += string.Format(@"<exclude sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", exclusion.Key.Id, target.Key.Id);
                        xml += "\n";
                    }
                }
            }
            xml += "</excludes>\n";

            // Includes
            xml += "<includes>\n";
            foreach (var inclusion in graph.IncludeExcludes)
            {
                foreach (var target in inclusion.Value)
                {
                    if (target.Value) // If it is an inclusion
                    {
                        xml += string.Format(@"<include sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", inclusion.Key.Id, target.Key.Id);
                        xml += "\n";
                    }
                }
            }
            xml += "</includes>\n";

            // Milestones
            xml += "<milestones>\n";
            foreach (var milestone in graph.Milestones)
            {
                foreach (var target in milestone.Value)
                {
                    xml += string.Format(@"<milestone sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", milestone.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</milestones>\n";
            // Spawns
            xml += "<spawns></spawns>\n";
            xml += "</constraints>\n";
            xml += "</specification>\n";
            // End constraints

            // Start states
            xml += @"<runtime>
<marking>
    <globalStore></globalStore>";
            xml += "\n";
            // Executed events
            xml += "<executed>\n";
            foreach (var activity in graph.Activities)
            {
                if (activity.Executed)
                {
                    xml += string.Format(@"<event id=""{0}""/>", activity.Id);
                    xml += "\n";
                }
            }
            xml += "</executed>\n";
            // Incuded events
            xml += "<included>\n";
            foreach (var activity in graph.Activities)
            {
                if (activity.Included)
                {
                    xml += string.Format(@"<event id=""{0}""/>", activity.Id);
                    xml += "\n";
                }
            }
            xml += "</included>\n";
            // Pending events
            xml += "<pendingResponses>\n";
            foreach (var activity in graph.Activities)
            {
                if (activity.Pending)
                {
                    xml += string.Format(@"<event id=""{0}""/>", activity.Id);
                    xml += "\n";
                }
            }
            xml += "</pendingResponses>\n";
            xml += @"</marking>
    <custom />
</runtime>";
            // End start states

            // End DCR Graph
            xml += "\n</dcrgraph>";
            return xml;
        }
    }
}
