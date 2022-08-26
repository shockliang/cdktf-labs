using System;
using System.Collections.Generic;
using System.Linq;

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
        
        /// <summary>
        /// https://www.terraform.io/language/functions/element
        /// </summary>
        /// <param name="items"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Element<T>(IEnumerable<T> items, int index)
        {
            return items.Count() <= index
                ? items.ElementAtOrDefault(index % items.Count())
                : items.ElementAtOrDefault(index);
        }

        public static T Coalesce<T>(params T[] items)
        {
            foreach (var item in items)
            {
                if (item == null) continue;
                if (item is string s && string.IsNullOrEmpty(s))
                {
                    continue;
                }
 
                return item;
                
            }

            throw new ArgumentNullException();
        }
    }
}