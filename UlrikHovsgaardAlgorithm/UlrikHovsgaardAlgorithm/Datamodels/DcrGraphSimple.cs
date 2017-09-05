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
        #region Initialization

        public DcrGraphSimple(HashSet<Activity> activities)
        {
            Activities = activities;
        }

        #endregion

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



        public int MakeActivityDisappear(Activity act)
        {
            Activities.Remove(act);

            // Remove relations
            return RemoveAllOccurrences(act, Includes, IncludesInverted)
                + RemoveAllOccurrences(act, Excludes, ExcludesInverted)
                + RemoveAllOccurrences(act, Responses, ResponsesInverted)
                + RemoveAllOccurrences(act, Conditions, ConditionsInverted);
        }

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

        public bool IsEverExecutable(Activity act)
        {
            return true;
        }

        public int RemoveAllIncomingIncludes(Activity act)
        {
            return RemoveAllIncoming(act, Includes, IncludesInverted);
        }

        public int RemoveAllIncomingExcludes(Activity act)
        {
            return RemoveAllIncoming(act, Excludes, ExcludesInverted);
        }

        public int RemoveAllIncomingResponses(Activity act)
        {
            return RemoveAllIncoming(act, Responses, ResponsesInverted);
        }

        public int RemoveAllIncomingConditions(Activity act)
        {
            return RemoveAllIncoming(act, Conditions, ConditionsInverted);
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

        private int RemoveAllOccurrences(Activity act,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            // Remove all outgoing from FWD-dict
            var removedRelations = dict.ContainsKey(act) ? dict[act].Count : 0;
            dict.Remove(act);

            removedRelations += RemoveAllIncoming(act, dict, dictInv);

            return removedRelations;
        }

        private int RemoveAllIncoming(
            Activity act,
            Dictionary<Activity, HashSet<Activity>> dict,
            Dictionary<Activity, HashSet<Activity>> dictInv)
        {
            var removedRelations = 0;
            HashSet<Activity> sources;
            if (dictInv.TryGetValue(act, out sources))
            {
                foreach (var sourceAct in sources)
                {
                    if (dict[sourceAct].Remove(act))
                        removedRelations++;
                }
            }
            
            dictInv.Remove(act);

            return removedRelations;
        }

        #endregion

        #endregion
    }
}
