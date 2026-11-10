using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class SynchronizedLibraryStore(ILibraryStore inner) : ILibraryStore
{
    private readonly object _gate = new();

    public IReadOnlyList<BookCopy> List() { lock (_gate) return inner.List(); }
    public BookCopy? GetById(string id) { lock (_gate) return inner.GetById(id); }
    public void Add(BookCopy item) { lock (_gate) inner.Add(item); }
    public void Update(BookCopy item) { lock (_gate) inner.Update(item); }
    public bool Remove(string id) { lock (_gate) return inner.Remove(id); }
    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) { lock (_gate) return inner.Find(predicate); }
}