using Core.Domain;
using Core.Exceptions;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests;

public sealed class EfLibraryStoreTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly List<AppDbContext> _contexts = new();

    public EfLibraryStoreTests()
    {
        _connection.Open();   
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        foreach (var c in _contexts) c.Dispose();
        _connection.Dispose();
    }

    private AppDbContext NewDb()
    {
        var db = new AppDbContext(_options);
        _contexts.Add(db);
        return db;
    }

    private EfLibraryStore NewStore() => new(NewDb());

    private static BookCopy Copy(string id, string isbn = "978-1", string title = "Кобзар",
        int year = 1840, string? author = "Шевченко", bool issued = false) =>
        BookCopy.Create(id, isbn, title, year, author, issued);

    [Fact]
    public void Add_ThenGetById_ReturnsEquivalentDomainObject()
    {
        NewStore().Add(Copy("C-1"));

        var loaded = NewStore().GetById("C-1");

        Assert.NotNull(loaded);
        Assert.Equal("Кобзар", loaded!.Title);
        Assert.Equal("Шевченко", loaded.Author);
        Assert.Equal(1840, loaded.Year);
        Assert.False(loaded.IsIssued);
    }

    [Fact]
    public void GetById_Unknown_ReturnsNull() =>
        Assert.Null(NewStore().GetById("nope"));

    [Fact]
    public void Add_TwoCopiesOfSameEdition_ShareOneBookRow()
    {
        NewStore().Add(Copy("C-1"));
        NewStore().Add(Copy("C-2"));

        using var db = NewDb();
        Assert.Equal(1, db.Books.Count());
        Assert.Equal(2, db.Copies.Count());
    }

    [Fact]
    public void Add_SameIsbnDifferentData_ThrowsDomainException_AndStoresNothingNew()
    {
        NewStore().Add(Copy("C-1"));

        Assert.Throws<DomainException>(() => NewStore().Add(Copy("C-2", title: "Інша назва")));

        Assert.Single(NewStore().List());
    }

    [Fact]
    public void Add_DuplicateId_ThrowsDomainException()
    {
        NewStore().Add(Copy("C-1"));

        Assert.Throws<DomainException>(() => NewStore().Add(Copy("C-1", isbn: "978-2", title: "Інша")));
    }

    [Fact]
    public void Update_IssuedState_IsPersisted()
    {
        NewStore().Add(Copy("C-1"));

        var copy = NewStore().GetById("C-1")!;
        copy.Issue();
        NewStore().Update(copy);

        Assert.True(NewStore().GetById("C-1")!.IsIssued);
    }

    [Fact]
    public void Update_EditionData_AppliesToAllCopiesOfThatIsbn()
    {
        NewStore().Add(Copy("C-1"));
        NewStore().Add(Copy("C-2"));

        NewStore().Update(Copy("C-1", title: "Нова назва"));

        Assert.Equal("Нова назва", NewStore().GetById("C-2")!.Title);
    }

    [Fact]
    public void Update_ToNewIsbn_MovesCopy_AndRemovesEmptyBook()
    {
        NewStore().Add(Copy("C-1", isbn: "ISBN-OLD"));

        NewStore().Update(Copy("C-1", isbn: "ISBN-NEW", title: "Нова", year: 2000));

        using var db = NewDb();
        Assert.Equal(new[] { "ISBN-NEW" }, db.Books.Select(b => b.Isbn).ToList());
        Assert.Equal("ISBN-NEW", NewStore().GetById("C-1")!.Isbn);
    }

    [Fact]
    public void Update_Unknown_ThrowsDomainException() =>
        Assert.Throws<DomainException>(() => NewStore().Update(Copy("nope")));

    [Fact]
    public void Remove_LastCopy_RemovesBookRow()
    {
        NewStore().Add(Copy("C-1"));

        Assert.True(NewStore().Remove("C-1"));

        using var db = NewDb();
        Assert.Empty(db.Books);
        Assert.Empty(db.Copies);
    }

    [Fact]
    public void Remove_OneOfTwoCopies_KeepsBookRow()
    {
        NewStore().Add(Copy("C-1"));
        NewStore().Add(Copy("C-2"));

        NewStore().Remove("C-1");

        using var db = NewDb();
        Assert.Equal(1, db.Books.Count());
        Assert.Equal(1, db.Copies.Count());
    }

    [Fact]
    public void Remove_Unknown_ReturnsFalse() =>
        Assert.False(NewStore().Remove("nope"));

    [Fact]
    public void List_IsOrderedById()
    {
        NewStore().Add(Copy("C-3", isbn: "978-3", title: "Три"));
        NewStore().Add(Copy("C-1", isbn: "978-1"));
        NewStore().Add(Copy("C-2", isbn: "978-2", title: "Два"));

        Assert.Equal(new[] { "C-1", "C-2", "C-3" }, NewStore().List().Select(b => b.Id));
    }

    [Fact]
    public void Find_FiltersInMemory_ByDomainPredicate()
    {
        NewStore().Add(Copy("C-1", isbn: "978-1", year: 1840));
        NewStore().Add(Copy("C-2", isbn: "978-2", title: "Нова", year: 2020));

        var found = NewStore().Find(b => b.Year > 2000);

        Assert.Equal(new[] { "C-2" }, found.Select(b => b.Id));
    }

    [Fact]
    public void Migrations_ApplyOnEmptyDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"lib_{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={path}").Options;
            using (var db = new AppDbContext(options))
            {
                db.Database.Migrate();   

                Assert.Empty(db.Database.GetPendingMigrations());
                Assert.Empty(db.Books);
                Assert.Empty(db.Copies);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            File.Delete(path);
        }
    }
}