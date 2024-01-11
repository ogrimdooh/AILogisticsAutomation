using System.Collections.Generic;
using System.Linq;

namespace AILogisticsAutomation
{
    public static class EnumerableExtension
    {

        public static IEnumerable<T> JoinChild<T>(this IEnumerable<IEnumerable<T>> source)
        {
            if (!source.Any())
                return Enumerable.Empty<T>();
            if (source.Count() > 1)
            {
                var item = source.ElementAt(0);
                for (int i = 1; i < source.Count(); i++)
                {
                    var item2 = source.ElementAt(i);
                    item = Enumerable.Concat(item, item2);
                }
                return item;
            }
            return source.First();
        }

    }

}