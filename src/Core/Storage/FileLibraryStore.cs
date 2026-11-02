using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class FileLibraryStore(string path) : ILibraryStore
{
    private readonly Dictionary<string, BookCopy> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;

        if (File.Exists(_path))
        {
            List<Dto.BookDto> dtos;
            try { dtos = CatalogJson.Read(_path); }
            catch (System.Text.Json.JsonException ex)
            {
                throw new InvalidDataException($"Файл даних пошкоджено: {_path}. {ex.Message}", ex);
            }

            _cache.Clear();
            for (var i = 0; i < dtos.Count; i++)
            {
                try
                {
                    var book = BookCopy.FromDto(dtos[i]);  
                    _cache[book.Id] = book;
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidDataException($"Файл {_path}, запис {i + 1}: {ex.Message}", ex);
                }
            }
        }
        _loaded = true;
    }

    private void Flush() =>
        CatalogJson.WriteAtomic(_path, _cache.Values.Select(b => b.ToDto()));

    public IReadOnlyList<BookCopy> List() { EnsureLoaded(); return _cache.Values.ToList(); }

    public BookCopy? GetById(string id) { EnsureLoaded(); return _cache.GetValueOrDefault(id); }

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();

        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Примірник з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(BookCopy item) { EnsureLoaded(); _cache[item.Id] = item; Flush(); }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id)) return false;
        Flush();
        return true;
    }

    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate)
    {
        EnsureLoaded();
        return _cache.Values.Where(predicate).ToList();
    }
}