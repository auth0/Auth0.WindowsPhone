using System.Collections.Generic;
namespace Auth0.SDK.Utils
{
    internal static class DictionaryExtensions
    {
        public static T2 ValueOrDefault<T1, T2>(this IDictionary<T1, T2> dict, T1 key, T2 def)
        {
            T2 ret;
            if (dict.TryGetValue(key, out ret))
            {
                return ret;
            }

            return def;
        }
    }
}
