using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CacheAspect
{
    public class InProcessMemoryCache : ICache
    {
        private Dictionary<string, object> cache = new Dictionary<string, object>();

        public object this[string key]
        {
            get
            { 
                return cache[key]; 
            }
            set
            {
                cache[key] = value;
            }
        }

        public bool Contains(string key)
        {
            return cache.ContainsKey(key);
        }

        public void Delete(string key)
        {
            cache.Remove(key);
        }

        public void DeleteSimilar(string key)
        {
            string[] keys = cache.Keys.ToArray();
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].StartsWith(key))
                {
                    cache.Remove(keys[i]);
                }
            }
        }
    }
}
