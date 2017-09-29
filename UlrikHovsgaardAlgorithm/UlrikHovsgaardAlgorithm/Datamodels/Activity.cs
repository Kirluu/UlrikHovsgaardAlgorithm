using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UlrikHovsgaardAlgorithm.Datamodels;

namespace UlrikHovsgaardAlgorithm.Data
{
    public class Activity
    {
        public string Id { get; }
        public string Name { get; }
        

        public Confidence IncludedConfidence { get; private set; } = new Confidence();
        public bool Included
        {
            get { return IncludedConfidence.Get > Threshold.Value; }
            set
            {
                if (IsNestedGraph)
                {
                    foreach (var act in NestedGraph.Activities)
                    {
                        act.Included = value;
                    }
                    IncludedConfidence = value ? new Confidence() {Invocations = 1, Violations = 1}: new Confidence();
                }
                else // TODO: If set to false, should still get invocation??
                {
                    IncludedConfidence = value ? new Confidence() { Invocations = 1, Violations = 1 } : new Confidence();
                }
            }
        }

        public bool IncrementExcludedInvocation()
        {
            return IncludedConfidence.IncrInvocations();
        }

        public bool IncrementExcludedViolation()
        {
            return IncludedConfidence.IncrViolations();
        }

        public bool Executed { get; set; }

        public Confidence PendingConfidence { get; private set; } = new Confidence();
        public bool Pending
        {
            get { return PendingConfidence.Get <= Threshold.Value; }
            set
            {
                if (IsNestedGraph)
                {
                    foreach (var act in NestedGraph.Activities)
                    {
                        act.Pending = value;
                    }
                }
                else
                {
                    PendingConfidence = value ? new Confidence() : new Confidence() { Invocations = 1, Violations = 1 };
                }
            }
        }


        public bool IncrementPendingInvocation()
        {
            return PendingConfidence.IncrInvocations();
        }
        public bool IncrementPendingViolation()
        {
            return PendingConfidence.IncrViolations();
        }


        public readonly bool IsNestedGraph;
        public string Roles { get; set; } = "";
        public readonly DcrGraph NestedGraph;


        public Activity(string id, string name)
        {
            var regex = new Regex(@"^[\w- ]+$");
            if (regex.IsMatch(id) == false)
            {
                throw new ArgumentException("The ID value provided must consist of only unicode letters and numbers and spaces.");
            }
            Id = id;
            Name = name;
            IsNestedGraph = false;
        }

        public Activity(string nameAndId)
        {
            var regex = new Regex(@"^[\w- ]+$");
            if (regex.IsMatch(nameAndId) == false)
            {
                throw new ArgumentException("The ID value provided must consist of only unicode letters and numbers and spaces.");
            }
            Id = nameAndId;
            Name = nameAndId;
            IsNestedGraph = false;
        }

        public Activity(string id, string name, DcrGraph nestedDcrGraph)
        {
            var regex = new Regex("^[\\w- ]+$");
            if (regex.IsMatch(id) == false)
            {
                throw new ArgumentException("The ID value provided must consist of only unicode letters and numbers and spaces.");
            }
            Id = id;
            Name = name;
            IsNestedGraph = true;
            NestedGraph = nestedDcrGraph;
        }

        public Activity Copy()
        {
            if (IsNestedGraph)
            {
                return new Activity(this.Id, this.Name, this.NestedGraph.Copy());
            }
            else
                return new Activity(this.Id, this.Name)
                {
                    Roles = this.Roles,
                    Executed = this.Executed,
                    Included = this.Included,
                    Pending = this.Pending
                };
        }

        public byte HashActivity(bool canExecute)
        {
            byte b = (byte)(Executed ? 1 << 2 : 0); // 00000100

            b += (byte)(canExecute ? 1 << 3 : 0);           // 00001000

            b += (byte)(Included ? 1 << 1 : 0);     // 00000010

            b += (byte)(Pending ? 1 : 0);         // 00000001

            return b;
        }

        public override bool Equals(object obj)
        {
            Activity otherActivity = obj as Activity;
            if (otherActivity != null)
            {
                return this.Id.Equals(otherActivity.Id);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + Id.GetHashCode();
                //hash = hash * 23 + Name.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (IsNestedGraph)
                return NestedGraph.Activities.Aggregate("Nested graph activities of " + this.Id + " : \n", (current,a) => current + "\t" + a+ "\n");

            return Id + " : " + Name + " inc=" + Included + ", pnd=" + Pending + ", exe=" + Executed;
        }

        public string ToDcrFormatString(bool printStatistics = false)
        {
            if (printStatistics)
                return (!Included ? ("%") : "") + (Pending ? "!" : "") + Id + " (Excluded: " + IncludedConfidence + ") (Pending: " + PendingConfidence + ")";
            else
                return (!Included ? ("%") : "") + (Pending ? "!" : "") + Id;
        }

        public string ExportToXml()
        {
            if (IsNestedGraph)
            {
                var xml = string.Format(@"<event id=""{0}"" scope=""private"" >", Id);
                xml = NestedGraph.Activities.Aggregate(xml, (current, nestedActivity) => current + nestedActivity.ExportToXml());
                xml += "</event>";
                return xml;
            }
            // else
            return string.Format(@"<event id=""{0}"" scope=""private"" >
                    <custom>
                        <visualization>
                            <location />
                        </visualization>
                        <roles>
                            <role>{1}</role>
                        </roles>
                        <groups>
                            <group />
                        </groups>
                        <eventType></eventType>
                        <eventDescription></eventDescription>
                        <level>1</level>
                        <eventData></eventData>
                    </custom>
                </event>", Id, Roles);
        }

        public string ExportLabelsToXml()
        {
            var thisLabel = string.Format(@"<label id =""{0}""/>", Name);
            if (IsNestedGraph)
            {
                return NestedGraph.Activities.Aggregate(thisLabel, (current, nestedActivity) => current + nestedActivity.ExportLabelsToXml());
            }
            // else
            return thisLabel;
        }

        public string ExportLabelMappingsToXml()
        {
            var thisLabelMapping = string.Format(@"<labelMapping eventId =""{0}""  labelId = ""{1}""/>", Id, Name);
            if (IsNestedGraph)
            {
                return NestedGraph.Activities.Aggregate(thisLabelMapping, (current, nestedActivity) => current + nestedActivity.ExportLabelMappingsToXml());
            }
            // else
            return thisLabelMapping;
        }
    }
}
