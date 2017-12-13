using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;

namespace UlrikHovsgaardAlgorithm.Utils
{
    public static class GraphProperties
    {
        // TODO: Use proper caching for this DcrSimple, since chasing occurs anyway - avoid N chases all resolving the same
        public static bool IsEverExecutable(DcrGraphSimple dcr, Activity act)
        {
            return IsEverExecutableInner(dcr, act, new Dictionary<Activity, bool>(), new HashSet<Activity>());
        }

        private static bool IsEverExecutableInner(DcrGraphSimple dcr, Activity act, Dictionary<Activity, bool> isEverExecutableCache, HashSet<Activity> visitedActivities)
        {
            /* Ideas:
            Of course requires graph-search. - Will this be a time-issue for the pattern-algorithm?
            (Chasing relations*2 from every activity - what is upper bound of this search?)

            - No ingoing conditions where !IsEverExecutable or never excluded by someone who IsEverExecutable
            - If Excluded, must be Included by someone who IsEverExecutable
            - Future work: Considerance of Milestones as well
            */

            // APPLICATION OF IDEAS:

            if (!visitedActivities.Add(act)) // if already present in set
            {
                // Already visited this node
                if (isEverExecutableCache.TryGetValue(act, out var executable))
                    return executable;
                else
                    // Backup value when we see the activity again - we don't know any better when meeting a cycle - assume the activity can be run
                    return true;
            }

            // If an activity has a condition to itself or is otherwise part of a condition-chain,
            // it can never be executed (shortcut check - avoid recursion of IsEverExecutable where possible)
            if (IsInConditionChain(dcr, act))
            {
                return isEverExecutableCache[act] = false;
            }

            // If excluded ...
            if (!act.Included)
            {
                // ... and never included ...
                if (act.IncludesMe(dcr).Count == 0)
                {
                    return isEverExecutableCache[act] = false;
                }

                // ... by an _executable_ activity
                if (act.IncludesMe(dcr).All(x => !IsEverExecutableInner(dcr, x, isEverExecutableCache, visitedActivities)))
                {
                    return isEverExecutableCache[act] = false;
                }
            }

            // TODO: Isn't this exactly the same as checking for Condition-Chain ??? - Isolated condition-chain may even be an "upgrade" ?
            var incomingConditions = act.ConditionsMe(dcr); // empty if none
            if (incomingConditions.Count > 0)
            {
                // Check for all incoming conditions
                foreach (var conditionSource in incomingConditions)
                {
                    var neverExcluded = false;
                    // If there are incoming exclusions to the condition targeting 'act' ...
                    if (dcr.ExcludesInverted.TryGetValue(conditionSource, out var incomingExcludesConditionSource))
                    {
                        // To be never executable, all excluders of any conditioner need to be never executable.
                        neverExcluded = incomingExcludesConditionSource.Count == 0 || incomingExcludesConditionSource.All(x =>
                                            !IsEverExecutableInner(dcr, x, isEverExecutableCache, visitedActivities));
                    }

                    // If the condition's source can never become Executed (making the condition no longer hold)
                    // AND if the condition source is also never set to Excluded by an EXECUTABLE activity (Exclude-relation).
                    if (conditionSource.Included && neverExcluded && !IsEverExecutableInner(dcr, conditionSource, isEverExecutableCache, visitedActivities))
                    {
                        return isEverExecutableCache[act] = false;
                    }
                }
            }

            return isEverExecutableCache[act] = true;
        }

        public static bool IsInConditionChain(DcrGraphSimple dcr, Activity act)
        {
            // First: Check whether this activity has a self-condition: Then definitely never executable!
            if (act.HasConditionTo(act, dcr))
                return true;

            // Otherwise, check whether any of those who have a condition to "act" are part of an always-Included condition-chain:
            return act.ConditionsMe(dcr).Any(incomingConditionAct => IsInConditionChainInner(dcr, incomingConditionAct, new HashSet<Activity> { act }));
        }

        /// <summary>
        /// Checks whether THIS activity is part of an always-Included condition-chain.
        /// </summary>
        private static bool IsInConditionChainInner(DcrGraphSimple dcr, Activity act, HashSet<Activity> conditionChainVisitedActs)
        {
            // FIRST: Assumptions: We are Included and never Excluded by others:
            if (!act.Included || act.ExcludesMe(dcr).Count > 0) // Idea: Count excluders that are EverExecutable - causes StackOverflow unless careful, though!
            {
                return false;
            }

            // If we've seen this activity in the chase before, this activity forms the chain!
            if (!conditionChainVisitedActs.Add(act))
            {
                return true;
            }

            foreach (var other in act.ConditionsMe(dcr))
            {
                // Self-conditions are an implicit condition-chain
                if (other.Equals(act)) return true;

                // If an ingoing condition is in a condition chain, (and you implicitly depend on this chain), then you are also in that condition-chain.
                if (IsInConditionChainInner(dcr, other, conditionChainVisitedActs))
                    return true;
            }

            return false;
        }
    }
}
