using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;

namespace UlrikHovsgaardAlgorithm.Datamodels
{
    public static class ActivityExtensions
    {
        private static Boolean IsIn(Activity target, Activity act, Dictionary<Activity, HashSet<Activity>> dict)
        {
            return act != null && (dict.TryGetValue(act, out var set) && set.Contains(act));
        }
        public static Boolean HasIncludeTo(this Activity thisActivity, Activity thatActivity, DcrGraphSimple dcr)
        {
            return IsIn(thatActivity, thisActivity, dcr.Includes);
        }

        public static Boolean HasExcludeTo(this Activity thisActivity, Activity thatActivity, DcrGraphSimple dcr)
        {
            return IsIn(thatActivity, thisActivity, dcr.Excludes);
        }

        public static Boolean HasConditionTo(this Activity thisActivity, Activity thatActivity, DcrGraphSimple dcr)
        {
            return IsIn(thatActivity, thisActivity, dcr.Conditions);
        }

        public static Boolean HasResponseTo(this Activity thisActivity, Activity thatActivity, DcrGraphSimple dcr)
        {
            return IsIn(thatActivity, thisActivity, dcr.Responses);
        }

        public static Boolean HasIncludeFrom(this Activity thisActivity, Activity thatActivity, DcrGraphSimple dcr)
        {
            return IsIn(thisActivity, thatActivity, dcr.IncludesInverted);
        }

        public static Boolean HasExcludeFrom(this Activity thisActivity, Activity thatActivity, DcrGraphSimple dcr)
        {
            return IsIn(thisActivity, thatActivity, dcr.ExcludesInverted);
        }

        public static Boolean HasConditionFrom(this Activity thisActivity, Activity thatActivity, DcrGraphSimple dcr)
        {
            return IsIn(thatActivity, thisActivity, dcr.ConditionsInverted);
        }

        public static Boolean HasResponseFrom(this Activity thisActivity, Activity thatActivity, DcrGraphSimple dcr)
        {
            return IsIn(thisActivity, thatActivity, dcr.ResponsesInverted);
        }

        public static HashSet<Activity> Includes(this Activity thisActivity, DcrGraphSimple dcr)
        {
            return dcr.Includes.TryGetValue(thisActivity, out var res) ? res : new HashSet<Activity>();
        }

        public static HashSet<Activity> Excludes(this Activity thisActivity, DcrGraphSimple dcr)
        {
            return dcr.Excludes.TryGetValue(thisActivity, out var res) ? res : new HashSet<Activity>();
        }

        public static HashSet<Activity> Conditions(this Activity thisActivity, DcrGraphSimple dcr)
        {
            return dcr.Conditions.TryGetValue(thisActivity, out var res) ? res : new HashSet<Activity>();
        }

        public static HashSet<Activity> Responses(this Activity thisActivity, DcrGraphSimple dcr)
        {
            return dcr.Responses.TryGetValue(thisActivity, out var res) ? res : new HashSet<Activity>();
        }

        public static HashSet<Activity> IncludesMe(this Activity thisActivity, DcrGraphSimple dcr)
        {
            return dcr.IncludesInverted.TryGetValue(thisActivity, out var res) ? res : new HashSet<Activity>();
        }
        public static HashSet<Activity> ExcludesMe(this Activity thisActivity, DcrGraphSimple dcr)
        {
            return dcr.ExcludesInverted.TryGetValue(thisActivity, out var res) ? res : new HashSet<Activity>();
        }
        public static HashSet<Activity> ConditionsMe(this Activity thisActivity, DcrGraphSimple dcr)
        {
            return dcr.ConditionsInverted.TryGetValue(thisActivity, out var res) ? res : new HashSet<Activity>();
        }
        public static HashSet<Activity> ResponsesMe(this Activity thisActivity, DcrGraphSimple dcr)
        {
            return dcr.ResponsesInverted.TryGetValue(thisActivity, out var res) ? res : new HashSet<Activity>();
        }
    }
}
