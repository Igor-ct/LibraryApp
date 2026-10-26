using System;
using System.Collections.Generic;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Exceptions;

namespace Core.Services;

public sealed class LendingService(ILibraryStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly ILibraryStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public event EventHandler<LibraryChangedEventArgs>? Changed;

    private void OnChanged(string id, LibraryChangeKind kind)
    {
        var args = new LibraryChangedEventArgs(id, kind);

        Changed?.Invoke(this, args);

        if (sinks != null)
        {
            foreach (var sink in sinks)
            {
                sink.Notify(args);
            }
        }
    }

    public BookCopy AddBook(string isbn, string title, int year, string? author = null)
    {
        string shortId = $"C-{Guid.NewGuid().ToString("N")[..6]}";

        var copy = BookCopy.Create(shortId, isbn, title, year, author);
        _store.Add(copy);

        OnChanged(copy.Id, LibraryChangeKind.BookAdded);

        return copy;
    }

    public void IssueCopy(string id)
    {
        var copy = _store.GetById(id) ?? throw new DomainException($"Немає примірника з id={id}.");

        copy.Issue();
        _store.Update(copy);

        OnChanged(copy.Id, LibraryChangeKind.CopyIssued);
    }

    public void ReturnCopy(string id)
    {
        var copy = _store.GetById(id) ?? throw new DomainException($"Немає примірника з id={id}.");

        copy.Return();
        _store.Update(copy);

        OnChanged(copy.Id, LibraryChangeKind.CopyReturned);
    }

    public async Task<int> ImportAsync(IBookImporter importer, string path,
    IProgress<int>? progress = null, CancellationToken ct = default)
    {
        var added = 0;

        await foreach (var item in importer.LoadAsync(path, progress, ct))
        {
            if (_store.GetById(item.Id) is not null) continue;

            var book = BookCopy.Create(item.Id, item.Isbn, item.Title, item.Year, item.Author);

            _store.Add(book);
            OnChanged(book.Id, LibraryChangeKind.BookAdded);
            added++;
        }

        progress?.Report(100);
        return added;
    }

    public IReadOnlyList<BookCopy> All() => _store.List();
    public BookCopy? Find(string id) => _store.GetById(id);
}