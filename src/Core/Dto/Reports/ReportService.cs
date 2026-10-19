using System;
using System.Collections.Generic;
using System.Linq;
using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<LibraryReportLine> OldBooks(IEnumerable<BookCopy> source, int yearThreshold) =>
        source.Where(b => b.Year < yearThreshold)
              .OrderBy(b => b.Year).ThenBy(b => b.Id, StringComparer.Ordinal)
              .Select(b => new LibraryReportLine(b.Id, b.Title, b.Year, b.Author ?? "Невідомо", b.IsIssued ? "Видано" : "Доступно"))
              .ToList();

    public static IReadOnlyList<LibraryReportLine> TopNewest(IEnumerable<BookCopy> source, int n) =>
        source.TopBy(b => b.Year, b => b.Id, n)
              .Select(b => new LibraryReportLine(b.Id, b.Title, b.Year, b.Author ?? "Невідомо", b.IsIssued ? "Видано" : "Доступно"))
              .ToList();

    public static IReadOnlyList<GroupSummary> ByAuthor(IEnumerable<BookCopy> source)
    {
        var query = from book in source
                    let author = book.Author ?? "Невідомий автор"
                    group book by author into g
                    orderby g.Count() descending
                    select new GroupSummary(g.Key, g.Count(), g.Min(b => b.Year));

        return query.ToList();
    }

    public static (int Items, int Available, double AverageYear, int MaxYear) Totals(IEnumerable<BookCopy> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count,
               list.Count(b => !b.IsIssued),
               list.Average(b => b.Year),
               list.Max(b => b.Year));
    }

    public static IReadOnlyList<LendingReportLine> GetActiveLendings(IEnumerable<BookCopy> books, IEnumerable<MockLendingRecord> lendings)
    {
        return books.Join(
            lendings,
            book => book.Id,
            lending => lending.BookId,
            (book, lending) => new LendingReportLine(book.Id, book.Title, lending.Reader, lending.Days)
        ).ToList();
    }
}

public record MockLendingRecord(string BookId, string Reader, int Days);