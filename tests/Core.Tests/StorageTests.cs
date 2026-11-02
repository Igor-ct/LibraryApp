using Core.Configuration;
using Core.Domain;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class StorageTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"lib_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void ResolveFullPath_Relative_UsesBaseDir_AndCreatesDirectory()
    {
        var o = new StorageOptions { DataPath = "data", FileName = "a.json" };

        var full = o.ResolveFullPath(_dir);

        Assert.Equal(Path.GetFullPath(Path.Combine(_dir, "data", "a.json")), full);
        Assert.True(Directory.Exists(Path.Combine(_dir, "data")));
    }

    [Fact]
    public void ResolveFullPath_Absolute_IgnoresBaseDir()
    {
        var absolute = Path.Combine(_dir, "abs");
        var o = new StorageOptions { DataPath = absolute, FileName = "a.json" };

        var full = o.ResolveFullPath(Path.Combine(_dir, "other"));

        Assert.Equal(Path.GetFullPath(Path.Combine(absolute, "a.json")), full);
    }

    [Fact]
    public void FileStore_PersistsBetweenInstances_AndCreatesMissingDirectory()
    {
        var path = Path.Combine(_dir, "nested", "lib.json");
        new FileLibraryStore(path).Add(BookCopy.Create("C-1", "111", "Кобзар", 1840 + 100, "Шевченко"));

        var loaded = new FileLibraryStore(path).GetById("C-1");

        Assert.NotNull(loaded);
        Assert.Equal("Кобзар", loaded!.Title);
    }

    [Fact]
    public void FileStore_WritesReadableCyrillic_CamelCase_AndLeavesNoTmp()
    {
        var path = Path.Combine(_dir, "lib.json");
        new FileLibraryStore(path).Add(BookCopy.Create("C-1", "111", "Кобзар", 1940, "Шевченко"));

        var text = File.ReadAllText(path);

        Assert.Contains("Кобзар", text);
        Assert.DoesNotContain("\\u", text);
        Assert.Contains("\"title\"", text);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void FileStore_CorruptedJson_ThrowsInvalidDataException()
    {
        var path = Path.Combine(_dir, "lib.json");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(path, "{ це не json");

        Assert.Throws<InvalidDataException>(() => new FileLibraryStore(path).List());
    }

    [Fact]
    public void FileStore_InvalidRecord_ThrowsInvalidDataException_WithRecordNumber()
    {
        var path = Path.Combine(_dir, "lib.json");
        Directory.CreateDirectory(_dir);
        File.WriteAllText(path,
            """[{"id":"C-1","isbn":"1","title":"A","year":2000},{"id":"C-2","isbn":"","title":"B","year":2000}]""");

        var ex = Assert.Throws<InvalidDataException>(() => new FileLibraryStore(path).List());

        Assert.Contains("запис 2", ex.Message);
    }
    [Fact]
    public void FileStore_UpdateAndRemove_ArePersisted()
    {
        var path = Path.Combine(_dir, "lib.json");
        var store = new FileLibraryStore(path);
        var book = BookCopy.Create("C-1", "111", "Кобзар", 1940, "Шевченко");
        store.Add(book);
        store.Add(BookCopy.Create("C-2", "222", "Лісова пісня", 1950, "Українка"));

        book.Issue();
        store.Update(book);
        store.Remove("C-2");

        var reloaded = new FileLibraryStore(path);
        Assert.True(reloaded.GetById("C-1")!.IsIssued);
        Assert.Null(reloaded.GetById("C-2"));
    }

    [Fact]
    public void WriteAtomic_OverwritesExistingFile_AndLeavesNoTmp()
    {
        var path = Path.Combine(_dir, "export.json");
        CatalogJson.WriteAtomic(path, new[] { BookCopy.Create("C-1", "1", "Стара", 1990).ToDto() });

        CatalogJson.WriteAtomic(path, new[] { BookCopy.Create("C-2", "2", "Нова", 2000).ToDto() });

        var text = File.ReadAllText(path);
        Assert.Contains("Нова", text);
        Assert.DoesNotContain("Стара", text);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void FileStore_DuplicateId_ThrowsAndKeepsFileUnchanged()
    {
        var path = Path.Combine(_dir, "lib.json");
        var store = new FileLibraryStore(path);
        store.Add(BookCopy.Create("C-1", "111", "Кобзар", 1940));

        Assert.Throws<InvalidOperationException>(() =>
            store.Add(BookCopy.Create("C-1", "222", "Інша", 1950)));

        Assert.Single(new FileLibraryStore(path).List());
    }
}