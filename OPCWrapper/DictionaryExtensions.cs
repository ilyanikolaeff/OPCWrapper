using System.Collections.Generic;
using System.Linq;

namespace OPCWrapper
{
    public static class DictionaryExtensions
    {
        public static IEnumerable<IDictionary<T, U>> Split<T, U>(this IDictionary<T, U> source, int splitCount)
        {
            return source.Select((pair, i) => new { Index = i, Value = pair })
                         .GroupBy(pair => pair.Index / splitCount)
                         .Select(v => v.Select(t => t.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }
}
