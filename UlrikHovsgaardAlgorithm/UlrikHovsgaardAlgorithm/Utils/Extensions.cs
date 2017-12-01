using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UlrikHovsgaardAlgorithm.Data;
using UlrikHovsgaardAlgorithm.Datamodels;

namespace UlrikHovsgaardAlgorithm.Utils
{
    public static class DictionaryExtensions
    {
        public static void AddOrUpdate<K, LV>(this Dictionary<K, (List<LV>, TimeSpan)> dict,
            K key,
            (List<LV>, TimeSpan) addValue)
        {
            var (addList, addTs) = addValue;
            if (dict.TryGetValue(key, out var val))
            {
                var (ls, ts) = val;
                dict[key] = (ls.Concat(addList).ToList(), ts + addTs);
            }
            else
            {
                dict[key] = (addList, addTs);
            }
        }

        public static void UpdateWith<K, LV>(this Dictionary<K, (List<LV>, TimeSpan)> dict,
            Dictionary<K, (List<LV>, TimeSpan)> other)
        {
            foreach (var kv in other)
            {
                dict.AddOrUpdate(kv.Key, kv.Value);
            }
        }
    }

    public static class DcrGraphSimpleExtensions
    {
        public static bool IsEverExecutable(this DcrGraphSimple dcr, Activity act)
        {
            return GraphProperties.IsEverExecutable(dcr, act);
        }

        public static bool IsInConditionChain(this DcrGraphSimple dcr, Activity act)
        {
            return GraphProperties.IsInConditionChain(dcr, act);
        }
    }
}
