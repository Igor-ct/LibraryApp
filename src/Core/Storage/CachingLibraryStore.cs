using System.Collections.Generic;
using System.Linq;
using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingLibraryStore(ILibraryStore innerStore) : ILibraryStore
{
    private Dictionary<string, BookCopy>? _cache;

    public IReadOnlyList<BookCopy> List()
    {
        _cache ??= innerStore.List().ToDictionary(p => p.Id);
        return _cache.Values.ToList();
    }

    public BookCopy? GetById(string id)
    {
        if (_cache is not null && _cache.TryGetValue(id, out var cached))
            return cached;

        var item = innerStore.GetById(id);
        if (item is not null)
        {
            _cache ??= [];
            _cache[id] = item;
        }
        return item;
    }

    public void Add(BookCopy item)
    {
        innerStore.Add(item);
        if (_cache is not null) _cache[item.Id] = item; 
    }

    public void Update(BookCopy item)
    {
        innerStore.Update(item);
        if (_cache is not null) _cache[item.Id] = item;
    }

    public bool Remove(string id)
    {
        bool removed = innerStore.Remove(id);
        if (removed && _cache is not null) _cache.Remove(id);
        return removed;
    }

    public IReadOnlyList<BookCopy> Find(System.Func<BookCopy, bool> predicate) =>
        List().Where(predicate).ToList();
}