using System.Collections.Generic;

namespace Radar.Tracking
{
    public static class DictionaryExtensions
    {
        public static V GetOrDefault<K, V>(this IDictionary<K, V> dic, K key) where V : new()
        {
            if (dic.ContainsKey(key))
            {
                return dic[key];
            }

            return new V();
        }

        public static void ReplaceOrAdd<K, V>(this IDictionary<K, V> dic, K key, V value) where V : new()
        {
            if (dic.ContainsKey(key))
            {
                dic[key] = value;
                return;
            }

            dic.Add(key, value);
        }
    }
}
