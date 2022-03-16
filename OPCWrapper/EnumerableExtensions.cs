using System.Collections.Generic;
using System.Linq;

namespace OPCWrapper
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> source, int splitCount)
        {
            return source
                   .Select((x, i) => new { Index = i, Value = x })
                   .GroupBy(x => x.Index / splitCount)
                   .Select(x => x.Select(v => v.Value).ToList());
        }
    }
}
