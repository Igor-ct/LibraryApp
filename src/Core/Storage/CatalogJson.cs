using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Dto;

namespace Core.Storage;

public static class CatalogJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
    };

    public static void WriteAtomic(string path, IEnumerable<BookDto> items)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        var tmp = full + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(items.ToList(), Options));
        File.Move(tmp, full, overwrite: true);  
    }

    public static List<BookDto> Read(string path)
    {
        var dtos = JsonSerializer.Deserialize<List<BookDto>>(File.ReadAllText(path), Options);
        return dtos ?? [];
    }
}