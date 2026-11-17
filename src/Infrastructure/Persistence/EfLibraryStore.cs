using Core.Abstractions;
using Core.Domain;
using Core.Exceptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class EfLibraryStore(AppDbContext db) : ILibraryStore
{
    private static BookCopy ToDomain(CopyRow row) =>
        BookCopy.Create(row.Id, row.Book.Isbn, row.Book.Title, row.Book.Year, row.Book.Author, row.IsIssued);

    private static BookRow ToBookRow(BookCopy c) =>
        new() { Isbn = c.Isbn, Title = c.Title, Author = c.Author, Year = c.Year };

    private static bool SameEdition(BookRow b, BookCopy c) =>
        b.Title == c.Title && b.Author == c.Author && b.Year == c.Year;

    private static DomainException EditionConflict(BookRow b) =>
        new($"ISBN {b.Isbn} уже зареєстровано для видання «{b.Title}» ({b.Year}). Дані видання мають збігатися.");

    private IQueryable<CopyRow> ReadQuery => db.Copies.AsNoTracking().Include(c => c.Book);

    public IReadOnlyList<BookCopy> List() =>
        ReadQuery.OrderBy(c => c.Id).AsEnumerable().Select(ToDomain).ToList();

    public BookCopy? GetById(string id) =>
        ReadQuery.SingleOrDefault(c => c.Id == id) is { } row ? ToDomain(row) : null;

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (db.Copies.Any(c => c.Id == item.Id))
            throw new DomainException($"Примірник з id={item.Id} уже існує.");

        var book = db.Books.SingleOrDefault(b => b.Isbn == item.Isbn);
        if (book is null)
        {
            book = ToBookRow(item);
            db.Books.Add(book);
        }
        else if (!SameEdition(book, item))
        {
            throw EditionConflict(book);
        }

        db.Copies.Add(new CopyRow { Id = item.Id, Book = book, IsIssued = item.IsIssued });
        Save();
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var row = db.Copies.Include(c => c.Book).SingleOrDefault(c => c.Id == item.Id)
                  ?? throw new DomainException($"Немає примірника з id={item.Id}.");

        using var tx = db.Database.BeginTransaction();

        if (row.Book.Isbn == item.Isbn)
        {
            row.Book.Title = item.Title;
            row.Book.Author = item.Author;
            row.Book.Year = item.Year;
        }
        else
        {
            var target = db.Books.SingleOrDefault(b => b.Isbn == item.Isbn);
            if (target is null)
            {
                target = ToBookRow(item);
                db.Books.Add(target);
            }
            else if (!SameEdition(target, item))
            {
                throw EditionConflict(target);
            }
            row.Book = target;
        }

        row.IsIssued = item.IsIssued;
        Save();
        PruneEmptyBooks();
        tx.Commit();
    }

    public bool Remove(string id)
    {
        var row = db.Copies.SingleOrDefault(c => c.Id == id);
        if (row is null) return false;

        using var tx = db.Database.BeginTransaction();
        db.Copies.Remove(row);
        Save();
        PruneEmptyBooks();
        tx.Commit();
        return true;
    }

    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) =>
        List().Where(predicate).ToList();

    private void PruneEmptyBooks() =>
        db.Books.Where(b => !b.Copies.Any()).ExecuteDelete();   

    private void Save()
    {
        try { db.SaveChanges(); }
        catch (DbUpdateException ex)
        {
            throw new DomainException("Не вдалося зберегти зміни: порушено обмеження цілісності даних.", ex);
        }
    }
}