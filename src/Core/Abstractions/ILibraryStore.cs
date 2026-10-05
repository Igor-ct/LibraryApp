using System.Collections.Generic;
using Core.Domain;

namespace Core.Abstractions;

public interface ILibraryStore
{
    IReadOnlyList<BookCopy> List();
    BookCopy? GetById(string id);
    void Add(BookCopy item);
    void Update(BookCopy item);
    bool Remove(string id);
    IReadOnlyList<BookCopy> Find(System.Func<BookCopy, bool> predicate);
}