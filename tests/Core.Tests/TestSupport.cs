using System.Runtime.CompilerServices;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Events;

namespace Core.Tests;

internal static class TestData
{
    public static IReadOnlyList<BookCopy> Items() => new[]
    {
        BookCopy.Create("C-001", "111", "Стара книга", 1990, "А"),
        BookCopy.Create("C-002", "222", "Середня",     2005, "Б"),
        BookCopy.Create("C-003", "333", "Нова",        2020, "В"),
    };
}

internal sealed class FakeBookImporter(int delayMs = 0, IReadOnlyList<BookDto>? items = null) : IBookImporter
{
    private readonly IReadOnlyList<BookDto> _items =
        items ?? new[] { new BookDto("B1", "123", "Title", 2020, "Author") };

    public async IAsyncEnumerable<BookDto> LoadAsync(
        string path,
        IProgress<int>? progress = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (delayMs > 0) await Task.Delay(delayMs, ct);
        ct.ThrowIfCancellationRequested();

        for (var i = 0; i < _items.Count; i++)
        {
            progress?.Report((i + 1) * 100 / _items.Count);
            yield return _items[i];
        }
    }
}

internal sealed class SpySink : INotificationSink
{
    public List<LibraryChangedEventArgs> Received { get; } = new();
    public void Notify(LibraryChangedEventArgs args) => Received.Add(args);
}