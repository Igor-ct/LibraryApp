using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Reports;

public static class ReportExtensions
{
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, string> tieBreaker,
        int n)
    {
        return source
            .OrderByDescending(keySelector)
            .ThenBy(tieBreaker, StringComparer.Ordinal)
            .Take(n);
    }
}