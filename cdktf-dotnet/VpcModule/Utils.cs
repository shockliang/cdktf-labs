using System.Collections.Generic;

namespace Cdktf.Dotnet.Aws
{
    public static class Utils
    {
        public static IDictionary<TKey, TValue> Merge<TKey, TValue>(params IDictionary<TKey, TValue>[] dictionaries)
        {
            var result = new Dictionary<TKey, TValue>();
            
            foreach (var dictionary in dictionaries)
            {
                foreach (var kvp in dictionary)
                {
                    if (!result.TryAdd(kvp.Key, kvp.Value))
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }

            return result;
        }
    }
}