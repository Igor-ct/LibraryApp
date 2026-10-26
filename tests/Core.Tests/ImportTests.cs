using Core.Dto;
using Core.Events;
using Core.Exceptions;
using Core.Import;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class ImportTests
{
    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new LendingService(new InMemoryLibraryStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new AsyncJsonBookImporter(), "any.json", null, cts.Token));
    }

    [Fact]
    public async Task ImportAsync_WithLinkedToken_ThrowsOnTimeout()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        var service = new LendingService(new InMemoryLibraryStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new FakeBookImporter(delayMs: 500), "fake.json", null, linkedCts.Token));

        Assert.False(userCts.IsCancellationRequested);
        Assert.True(timeoutCts.IsCancellationRequested);
    }

    [Fact]
    public async Task ImportAsync_AddsNewBooks_ReturnsCount()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var importer = new FakeBookImporter(items: new[]
        {
            new BookDto("B1", "111", "Книга 1", 2020, "А"),
            new BookDto("B2", "222", "Книга 2", 2021, "Б"),
        });

        var added = await service.ImportAsync(importer, "x.json");

        Assert.Equal(2, added);
        Assert.Equal(2, service.All().Count);
    }

    [Fact]
    public async Task ImportAsync_SkipsDuplicates()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var dto = new BookDto("B1", "111", "Книга", 2020, "А");

        await service.ImportAsync(new FakeBookImporter(items: new[] { dto }), "x.json");
        var second = await service.ImportAsync(new FakeBookImporter(items: new[] { dto }), "x.json");

        Assert.Equal(0, second);
        Assert.Single(service.All());
    }

    [Fact]
    public async Task ImportAsync_ReportsProgressUpTo100_Monotonically()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var reported = new List<int>();
        var progress = new SyncProgress<int>(reported.Add);
        var importer = new FakeBookImporter(items: new[]
        {
            new BookDto("B1", "1", "A", 2020, "X"),
            new BookDto("B2", "2", "B", 2021, "Y"),
        });

        await service.ImportAsync(importer, "x.json", progress);

        Assert.Equal(100, reported[^1]);
        Assert.Equal(reported.OrderBy(x => x), reported);
    }

    [Fact]
    public async Task ImportAsync_RaisesChangedForEachAddedBook()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var events = new List<LibraryChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        await service.ImportAsync(new FakeBookImporter(items: new[]
        {
            new BookDto("B1", "1", "A", 2020, "X"),
            new BookDto("B2", "2", "B", 2021, "Y"),
        }), "x.json");

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(LibraryChangeKind.BookAdded, e.Kind));
    }

    [Fact]
    public async Task Importer_InvalidRecord_ThrowsDomainException_WithRecordNumber()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lib_{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path,
            """
            [{"id":"B1","isbn":"1","title":"A","year":2020,"author":"X"},
             {"id":"B2","isbn":"","title":"B","year":2021,"author":"Y"}]
            """);
        try
        {
            var service = new LendingService(new InMemoryLibraryStore());

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new AsyncJsonBookImporter(), path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
        }
        finally
        {
            File.Delete(path);
        }
    }
}