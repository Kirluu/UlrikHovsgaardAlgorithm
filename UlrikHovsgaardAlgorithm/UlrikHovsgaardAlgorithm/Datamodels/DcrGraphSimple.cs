using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Datamodels
{
    public class DcrGraphSimple
    {
        #region Properties

        public string Title { get; set; }
        public HashSet<Activity> Activities { get; set; } = new HashSet<Activity>();
        public Dictionary<Activity, HashSet<Activity>> Responses { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Includes { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Excludes { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> Conditions { get; } = new Dictionary<Activity, HashSet<Activity>>();
        //public Dictionary<Activity, HashSet<Activity>> Milestones { get; } = new Dictionary<Activity, HashSet<Activity>>();

        public Dictionary<Activity, HashSet<Activity>> ResponsesInverted { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> IncludesInverted { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> ExcludesInverted { get; } = new Dictionary<Activity, HashSet<Activity>>();
        public Dictionary<Activity, HashSet<Activity>> ConditionsInverted { get; } = new Dictionary<Activity, HashSet<Activity>>();
        //public Dictionary<Activity, HashSet<Activity>> MilestonesRev { get; } = new Dictionary<Activity, HashSet<Activity>>();

        #endregion

        #region Methods

        public void AddInclude(string sourceId, string targetId)
        {
            AddActivitiesToRelationDictionary(sourceId, targetId, Includes, IncludesInverted);
        }

        public void AddExclude(string sourceId, string targetId)
        {
            AddActivitiesToRelationDictionary(sourceId, targetId, Excludes, ExcludesInverted);
        }

        public void AddResponse(string sourceId, string targetId)
        {
            AddActivitiesToRelationDictionary(sourceId, targetId, Responses, ResponsesInverted);
        }

        public void AddCondition(string sourceId, string targetId)
        {
            AddActivitiesToRelationDictionary(sourceId, targetId, Conditions, ConditionsInverted);
        }

        public void RemoveInclude(string sourceId, string targetId)
        {
            RemoveRelation(sourceId, targetId, Includes, IncludesInverted);
        }

        public void RemoveExclude(string sourceId, string targetId)
        {
            RemoveRelation(sourceId, targetId, Includes, IncludesInverted);
        }

        public void RemoveResponse(string sourceId, string targetId)
        {
            RemoveRelation(sourceId, targetId, Includes, IncludesInverted);
        }

        public void RemoveCondition(string sourceId, string targetId)
        {
            RemoveRelation(sourceId, targetId, Includes, IncludesInverted);
        }

        #region Private methods

        private void AddActivitiesToRelationDictionary(
            string sourceId,
            string targetId,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            var sourceAct = Activities.First(x => x.Id == sourceId);
            var targetAct = Activities.First(x => x.Id == targetId);

            // Forward
            HashSet<Activity> targets;
            if (dict.TryGetValue(sourceAct, out targets))
                targets.Add(targetAct);
            else
                dict.Add(sourceAct, new HashSet<Activity> { targetAct });

            // Inverted
            HashSet<Activity> sources;
            if (dictInv.TryGetValue(targetAct, out sources))
                sources.Add(sourceAct);
            else
                dictInv.Add(targetAct, new HashSet<Activity> { sourceAct });
        }

        private void RemoveRelation(
            string sourceId,
            string targetId,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            var sourceAct = Activities.First(x => x.Id == sourceId);
            var targetAct = Activities.First(x => x.Id == targetId);

            // Forward
            HashSet<Activity> targets;
            if (dict.TryGetValue(sourceAct, out targets))
                targets.Remove(targetAct);

            // Inverted
            HashSet<Activity> sources;
            if (dictInv.TryGetValue(targetAct, out sources))
                sources.Remove(sourceAct);
        }

        #endregion

        #endregion
    }
}
