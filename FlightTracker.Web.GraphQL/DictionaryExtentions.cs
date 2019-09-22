using System.Collections.Generic;

namespace FlightTracker.Web
{
    public static class DictionaryExtentions
    {
        public static bool TryGetValue<T>(this Dictionary<string, object> dictionary, string key, out T value)
        {
            if (dictionary.TryGetValue(key, out var valueObj) && valueObj is T v)
            {
                value = v;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
