using Core.Domain;
using Core.Reports;
using Xunit;

namespace Core.Tests;

public class ReportServiceTests
{
    private static BookCopy Book(string id, int year, string? author = "А", bool issued = false) =>
        BookCopy.Create(id, $"ISBN-{id}", $"Книга {id}", year, author, issued);

    private static List<BookCopy> Sample() => new()
    {
        Book("C-001", 1990, "А"),
        Book("C-002", 2005, "Б", issued: true),
        Book("C-003", 2020, "А"),
        Book("C-004", 2020, null),
    };


    [Theory]
    [InlineData(1990, 0)]   
    [InlineData(1991, 1)]
    [InlineData(2005, 1)]   
    [InlineData(2006, 2)]
    [InlineData(2021, 4)]
    public void OldBooks_UsesStrictlyLessThanThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.OldBooks(Sample(), threshold).Count);

    [Fact]
    public void OldBooks_EmptySource_ReturnsEmpty() =>
        Assert.Empty(ReportService.OldBooks(new List<BookCopy>(), 2100));

    [Fact]
    public void OldBooks_SortedByYear_ThenById()
    {
        var source = new[]
        {
            Book("C-9", 2000),
            Book("C-2", 1980),
            Book("C-1", 2000),
        };

        var ids = ReportService.OldBooks(source, 2100).Select(l => l.Id);

        Assert.Equal(new[] { "C-2", "C-1", "C-9" }, ids);
    }

    [Fact]
    public void OldBooks_MapsStatusAndAuthor()
    {
        var source = new[]
        {
            Book("C-1", 1990, null, issued: true),
            Book("C-2", 1991, "Франко", issued: false),
        };

        var lines = ReportService.OldBooks(source, 2000);

        Assert.Equal("Видано", lines[0].Status);
        Assert.Equal("Невідомо", lines[0].Author);
        Assert.Equal("Доступно", lines[1].Status);
        Assert.Equal("Франко", lines[1].Author);
    }


    [Fact]
    public void TopNewest_ReturnsNewestFirst_AndRespectsN()
    {
        var lines = ReportService.TopNewest(Sample(), 3);

        Assert.Equal(3, lines.Count);
        Assert.Equal(new[] { "C-003", "C-004" }, lines.Take(2).Select(l => l.Id).OrderBy(x => x));
        Assert.Equal("C-002", lines[2].Id);
    }

    [Fact]
    public void TopNewest_IsDeterministic_RegardlessOfInputOrder()
    {
        var forward = Sample();
        var reversed = Sample();
        reversed.Reverse();

        var a = ReportService.TopNewest(forward, 3).Select(l => l.Id).ToList();
        var b = ReportService.TopNewest(reversed, 3).Select(l => l.Id).ToList();

        Assert.Equal(a, b);
    }

    [Fact]
    public void TopNewest_NGreaterThanCount_ReturnsAll() =>
        Assert.Equal(4, ReportService.TopNewest(Sample(), 100).Count);

    [Fact]
    public void TopNewest_MapsStatusAndAuthor()
    {
        var lines = ReportService.TopNewest(new[] { Book("C-1", 2020, null, issued: true) }, 1);

        Assert.Single(lines);
        Assert.Equal("Видано", lines[0].Status);
        Assert.Equal("Невідомо", lines[0].Author);
    }


    [Fact]
    public void ByAuthor_GroupsCountsAndOldestYear_OrderedByCountDescending()
    {
        var groups = ReportService.ByAuthor(Sample());

        Assert.Equal(3, groups.Count);

        Assert.Equal("А", groups[0].Key);
        Assert.Equal(2, groups[0].Count);
        Assert.Equal(1990, groups[0].OldestYear);

        Assert.Equal("Б", groups[1].Key);
        Assert.Equal(2005, groups[1].OldestYear);

        Assert.Equal("Невідомий автор", groups[2].Key);
        Assert.Equal(2020, groups[2].OldestYear);
    }

    [Fact]
    public void ByAuthor_EmptySource_ReturnsEmpty() =>
        Assert.Empty(ReportService.ByAuthor(new List<BookCopy>()));


    [Fact]
    public void Totals_EmptySource_ReturnsZeros()
    {
        var t = ReportService.Totals(new List<BookCopy>());

        Assert.Equal(0, t.Items);
        Assert.Equal(0, t.Available);
        Assert.Equal(0d, t.AverageYear);
        Assert.Equal(0, t.MaxYear);
    }

    [Fact]
    public void Totals_ComputesAggregates()
    {
        var t = ReportService.Totals(Sample());

        Assert.Equal(4, t.Items);
        Assert.Equal(3, t.Available);             
        Assert.Equal(2008.75, t.AverageYear, 2);  
        Assert.Equal(2020, t.MaxYear);
    }

    [Fact]
    public void Totals_AllIssued_AvailableIsZero()
    {
        var source = new[] { Book("C-1", 2000, issued: true), Book("C-2", 2010, issued: true) };

        var t = ReportService.Totals(source);

        Assert.Equal(2, t.Items);
        Assert.Equal(0, t.Available);
    }


    [Fact]
    public void GetActiveLendings_JoinsOnlyMatchingBooks_AndMapsFields()
    {
        var lendings = new[]
        {
            new MockLendingRecord("C-001", "Іван Франко", 14),
            new MockLendingRecord("C-003", "Леся Українка", 5),
            new MockLendingRecord("X-999", "Немає такої книги", 1),
        };

        var result = ReportService.GetActiveLendings(Sample(), lendings);

        Assert.Equal(2, result.Count);

        var first = result.Single(l => l.BookId == "C-001");
        Assert.Equal("Книга C-001", first.Title);
        Assert.Equal("Іван Франко", first.ReaderName);
        Assert.Equal(14, first.DaysBorrowed);

        Assert.DoesNotContain(result, l => l.BookId == "X-999");
    }

    [Fact]
    public void GetActiveLendings_NoMatches_ReturnsEmpty()
    {
        var lendings = new[] { new MockLendingRecord("X-1", "Ніхто", 3) };

        Assert.Empty(ReportService.GetActiveLendings(Sample(), lendings));
    }
}