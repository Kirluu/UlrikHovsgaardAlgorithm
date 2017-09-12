using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;

namespace UlrikHovsgaardAlgorithm.Export
{
    public class DcrGraphExporter
    {
        #region Convert to DcrGraphSimple
        
        public static DcrGraphSimple ExportToSimpleDcrGraph(DcrGraph graph)
        {
            // TODO: Copy relations - use confidence - "DcrGraph.FilterDictionaryByThreshold"

            var newGraph = new DcrGraphSimple(graph.Activities);

            foreach (var response in graph.Responses)
                foreach (var actual in DcrGraph.FilterDictionaryByThreshold(response.Value))
                    newGraph.AddResponse(response.Key, actual);

            foreach (var condition in graph.Conditions)
                foreach (var actual in DcrGraph.FilterDictionaryByThreshold(condition.Value))
                    newGraph.AddCondition(condition.Key, actual);

            foreach (var relation in graph.IncludeExcludes)
                foreach (var target in relation.Value)
                    if (target.Value.IsAboveThreshold())
                        newGraph.AddInclude(relation.Key, target.Key);
                    else
                        newGraph.AddExclude(relation.Key, target.Key); 

            return newGraph;
        }

        #endregion

        #region XML

        public static string ExportToXml(DcrGraphSimple graph)
        {
            var xml = "<dcrgraph>\n";

            xml += "<specification>\n<resources>\n<events>\n"; // Begin events
            // Event definitions
            foreach (var activity in graph.Activities)
            {
                xml += activity.ExportToXml();
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
                xml += activity.ExportLabelsToXml();
                xml += "\n";
            }
            xml += "</labels>\n";
            // Label mappings
            xml += "<labelMappings>\n";
            foreach (var activity in graph.Activities)
            {
                xml += activity.ExportLabelMappingsToXml();
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
                    xml += string.Format(@"<condition sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", condition.Key.Id, target.Id);
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
            foreach (var exclusion in graph.Excludes)
            {
                foreach (var target in exclusion.Value)
                {
                    xml += string.Format(@"<exclude sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", exclusion.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</excludes>\n";

            // Includes
            xml += "<includes>\n";
            foreach (var inclusion in graph.Includes)
            {
                foreach (var target in inclusion.Value)
                {
                    xml += string.Format(@"<include sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", inclusion.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</includes>\n";

            // Milestones
            //xml += "<milestones>\n";
            //foreach (var milestone in graph.Milestones)
            //{
            //    foreach (var target in DcrGraph.FilterDictionaryByThreshold(milestone.Value))
            //    {
            //        xml += string.Format(@"<milestone sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", milestone.Key.Id, target.Id);
            //        xml += "\n";
            //    }
            //}
            //xml += "</milestones>\n";
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

        public static string ExportToXml(DcrGraph graph)
        {
            var xml = "<dcrgraph>\n";

            xml += "<specification>\n<resources>\n<events>\n"; // Begin events
            // Event definitions
            foreach (var activity in graph.Activities)
            {
                xml += activity.ExportToXml();
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
                xml += activity.ExportLabelsToXml();
                xml += "\n";
            }
            xml += "</labels>\n";
            // Label mappings
            xml += "<labelMappings>\n";
            foreach (var activity in graph.Activities)
            {
                xml += activity.ExportLabelMappingsToXml();
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
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(condition.Value))
                {
                    xml += string.Format(@"<condition sourceId=""{0}"" targetId=""{1}"" filterLevel=""1""  description=""""  time=""""  groups=""""  />", condition.Key.Id, target.Id);
                    xml += "\n";
                }
            }
            xml += "</conditions>\n";

            // Responses
            xml += "<responses>\n";
            foreach (var response in graph.Responses)
            {
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(response.Value))
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
                    if (target.Value.Get <= Threshold.Value) // If it is an exclusion
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
                    if (target.Value.Get > Threshold.Value && !inclusion.Key.Equals(target.Key)) // If it is an inclusion and source != target (avoid self-inclusion)
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
                foreach (var target in DcrGraph.FilterDictionaryByThreshold(milestone.Value))
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
            foreach (var activity in graph.GetActivities())
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
            foreach (var activity in graph.GetActivities())
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
            foreach (var activity in graph.GetActivities())
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

        #endregion
        

        #region Format-strings

        #region Relation filtering strings

        public const string AllRelationsStr = "All";
        public const string InclusionsExclusionsStr = "Inclusions/Exclusions";
        public const string ResponsesStr = "Responses";
        public const string ConditionsStr = "Conditions";

        #endregion

        /// <param name="activityFilter">Only prints relations and states related to these activities.</param>
        /// <param name="relationFilter">Only prints relations of this type</param>
        /// <param name="printStatistics"></param>
        /// <returns></returns>
        public string ToDcrFormatString(DcrGraph graph, HashSet<Activity> activityFilter, string relationFilter, bool printStatistics)
        {
            var returnString = "";

            foreach (var a in graph.Activities)
            {
                if (activityFilter.Contains(a))
                    returnString += a.ToDcrFormatString(printStatistics) + Environment.NewLine;
            }

            if (relationFilter == AllRelationsStr || relationFilter == InclusionsExclusionsStr)
            {
                foreach (var sourcePair in graph.IncludeExcludes)
                {
                    var source = sourcePair.Key;
                    if (!activityFilter.Contains(source)) continue;
                    foreach (var targetPair in sourcePair.Value)
                    {
                        if (!activityFilter.Contains(targetPair.Key)) continue;

                        var conf = targetPair.Value;
                        var incOrEx = conf.Get > Threshold.Value ? " -->+ " : " -->% ";
                        returnString += source.Id + incOrEx + targetPair.Key.Id + " (" + conf + ")" + Environment.NewLine;
                    }
                }
            }

            if (relationFilter == AllRelationsStr || relationFilter == ResponsesStr)
            {
                foreach (var sourcePair in graph.Responses)
                {
                    var source = sourcePair.Key;
                    if (!activityFilter.Contains(source)) continue;

                    foreach (var target in sourcePair.Value)
                    {
                        if (activityFilter.Contains(target.Key))
                            returnString += source.Id + " *--> " + target.Key.Id + " (" + target.Value + ")" + Environment.NewLine;
                    }
                }
            }

            if (relationFilter == AllRelationsStr || relationFilter == ConditionsStr)
            {
                foreach (var sourcePair in graph.Conditions)
                {
                    var source = sourcePair.Key;
                    if (!activityFilter.Contains(source)) continue;

                    foreach (var target in sourcePair.Value)
                    {
                        if (activityFilter.Contains(target.Key))
                            returnString += source.Id + " -->* " + target.Key.Id + " (" + target.Value + ")" +
                                            Environment.NewLine;
                    }
                }
            }

            //foreach (var sourcePair in graph.Milestones)
            //{
            //    var source = sourcePair.Key;
            //    if (!activityFilter.Contains(source)) continue;

            //    foreach (var target in FilterDictionaryByThresholdAsDictionary(sourcePair.Value))
            //    {
            //        if (activityFilter.Contains(target.Key))
            //            returnString += source.Id + " --><> " + target.Key.Id + " (" + target.Value + ")" + Environment.NewLine;
            //    }
            //}

            return returnString;
        }

        /// <param name="activityFilter">Only prints relations and states related to these activities.</param>
        /// <param name="relationFilter">Only prints relations of this type</param>
        /// <param name="printStatistics"></param>
        /// <returns></returns>
        public static List<Tuple<string, Confidence>> FilteredConstraintStringsWithConfidence(DcrGraph graph, HashSet<Activity> activityFilter, string relationFilter, bool printStatistics)
        {
            var res = new List<Tuple<string, Confidence>>();

            foreach (var a in graph.Activities)
            {
                if (activityFilter.Contains(a))
                {
                    res.Add(Tuple.Create(a.Id + " Excluded", a.IncludedConfidence));
                    res.Add(Tuple.Create(a.Id + " Pending", a.PendingConfidence));
                }
            }

            if (relationFilter == AllRelationsStr || relationFilter == InclusionsExclusionsStr)
            {
                foreach (var sourcePair in graph.IncludeExcludes)
                {
                    var source = sourcePair.Key;
                    if (!activityFilter.Contains(source)) continue;
                    foreach (var targetPair in sourcePair.Value)
                    {
                        if (!activityFilter.Contains(targetPair.Key)) continue;

                        res.Add(Tuple.Create(source.Id + " -->% " + targetPair.Key.Id, targetPair.Value));
                    }
                }
            }

            if (relationFilter == AllRelationsStr || relationFilter == ResponsesStr)
            {
                foreach (var sourcePair in graph.Responses)
                {
                    var source = sourcePair.Key;
                    if (!activityFilter.Contains(source)) continue;

                    foreach (var target in sourcePair.Value)
                    {
                        if (activityFilter.Contains(target.Key))
                            res.Add(Tuple.Create(source.Id + " *--> " + target.Key.Id, target.Value));
                    }
                }
            }

            if (relationFilter == AllRelationsStr || relationFilter == ConditionsStr)
            {
                foreach (var sourcePair in graph.Conditions)
                {
                    var source = sourcePair.Key;
                    if (!activityFilter.Contains(source)) continue;

                    foreach (var target in sourcePair.Value)
                    {
                        if (activityFilter.Contains(target.Key))
                            res.Add(Tuple.Create(source.Id + " -->* " + target.Key.Id, target.Value));
                    }
                }
            }

            //foreach (var sourcePair in graph.Milestones)
            //{
            //    var source = sourcePair.Key;
            //    if (!activityFilter.Contains(source)) continue;

            //    foreach (var target in FilterDictionaryByThresholdAsDictionary(sourcePair.Value))
            //    {
            //        if (activityFilter.Contains(target.Key))
            //            returnString += source.Id + " --><> " + target.Key.Id + " (" + target.Value + ")" + Environment.NewLine;
            //    }
            //}

            return res;
        }

        #endregion
    }
}
