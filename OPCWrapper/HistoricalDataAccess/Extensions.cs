using System;
using System.Collections.Generic;
using System.Linq;

namespace OPCWrapper.HistoricalDataAccess
{
    public static class Extensions
    {
        public static OpcHdaResultItem GetLastGoodResult(this IEnumerable<OpcHdaResultItem> results)
        {
            foreach (var result in results.Reverse())
            {
                if (result.Quality >= 192)
                    return result;
            }
            return null;
        }

        public static OpcHdaResultItem GetFirstGoodResult(this IEnumerable<OpcHdaResultItem> results)
        {
            foreach (var result in results)
            {
                if (result.Quality >= 192)
                    return result;
            }
            return null;
        }


        public static IEnumerable<OpcHdaResultItem> FilterResults(this IEnumerable<OpcHdaResultItem> results, FilterType filterType)
        {
            var historyResults = new List<OpcHdaResultItem>();

            if (filterType == FilterType.ValueNotNull)
                return results.Where(p => p.Value != null);
            if (filterType == FilterType.QualityGood)
                return results.Where(p => p.Quality >= 192);
            if (filterType == FilterType.GoodAndNotNull)
                return results.Where(p => (p.Quality >= 192) && (p.Value != null)).ToList();

            return null;
        }

        public static OpcHdaResultItem FindClosestResult(this IEnumerable<OpcHdaResultItem> results, DateTime searchTimestamp)
        {
            OpcHdaResultItem closestResult = null;
            long min = long.MaxValue;
            foreach (var result in results)
            {
                if (Math.Abs(result.Timestamp.Ticks - searchTimestamp.Ticks) < min)
                {
                    min = Math.Abs(result.Timestamp.Ticks - searchTimestamp.Ticks);
                    closestResult = result;
                }
            }
            return closestResult;
        }

        public static OpcHdaResultItem FindClosestResultByValue(this IEnumerable<OpcHdaResultItem> results, object searchValue, DateTime searchTimestamp)
        {
            // выбираем результаты которые равны искомому результату
            var searchCollection = results.FilterResults(FilterType.QualityGood).Where(p => p.Value == searchValue);
            return searchCollection.FindClosestResult(searchTimestamp);
        }

        public static OpcHdaResultItem GetResult(this IEnumerable<OpcHdaResultItem> results, DateTime startTimestamp, DateTime endTimestamp, FindType findType)
        {
            // truncate timestamps
            startTimestamp = startTimestamp.Truncate(TimeSpan.TicksPerSecond);
            endTimestamp = endTimestamp.Truncate(TimeSpan.TicksPerSecond);
            // take results in range of timestamps
            var areaResults = results.Where(p => p.Timestamp.Truncate(TimeSpan.TicksPerSecond) >= startTimestamp && p.Timestamp.Truncate(TimeSpan.TicksPerSecond) <= endTimestamp);
            if (findType == FindType.First)
                return areaResults.FirstOrDefault();
            if (findType == FindType.Last)
                return areaResults.LastOrDefault();

            return null;
        }

        public static IEnumerable<OpcHdaResultItem> GetRangeOfResults(this IEnumerable<OpcHdaResultItem> results, DateTime startTimestamp, DateTime endTimestamp, bool includeBoundValues)
        {
            var resultsList = results.ToList();
            var range = new List<OpcHdaResultItem>();
            for (int i = 0; i < resultsList.Count() - 1; i++)
            {
                if (CheckTimestamp(resultsList[i].Timestamp, startTimestamp, endTimestamp))
                {
                    range.Add(resultsList[i]);
                }

                if (includeBoundValues)
                {
                    // left 
                    if (!CheckTimestamp(resultsList[i].Timestamp, startTimestamp, endTimestamp) && CheckTimestamp(resultsList[i + 1].Timestamp, startTimestamp, endTimestamp))
                        range.Add(resultsList[i]);
                    // right
                    if (CheckTimestamp(resultsList[i].Timestamp, startTimestamp, endTimestamp) && !CheckTimestamp(resultsList[i + 1].Timestamp, startTimestamp, endTimestamp))
                        range.Add(resultsList[i]);
                }
            }
            return range;
        }

        private static bool CheckTimestamp(DateTime checkTimestamp, DateTime startTimestamp, DateTime endTimestamp)
        {
            var trCheckTS = checkTimestamp.Truncate(TimeSpan.TicksPerSecond);
            var trStartTimestamp = startTimestamp.Truncate(TimeSpan.TicksPerSecond);
            var trEndTimestamp = endTimestamp.Truncate(TimeSpan.TicksPerSecond);
            return trCheckTS >= trStartTimestamp && trCheckTS <= trEndTimestamp;
        }


        public static OpcHdaResultItem GetPreviousResult(this IEnumerable<OpcHdaResultItem> values, OpcHdaResultItem element)
        {
            var valuesList = values.ToList();
            int index = valuesList.IndexOf(element);
            if (index == -1)
                return null;
            else if (index == 0)
                return valuesList[0];
            else
                return valuesList[index - 1];
        }
    }
}
