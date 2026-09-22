using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    public static ImportResult<BookDto> Load(string path)
    {
        var items = new List<BookDto>();
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path);

            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add("Фатальна помилка: JSON має бути масивом об'єктів");
                return new ImportResult<BookDto>(items, errors);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            int number = 1;

            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                int propCount = element.EnumerateObject().Count();
                if (propCount > 5) 
                {
                    errors.Add($"запис {number}: занадто багато параметрів, отримав {propCount}");
                    number++;
                    continue;
                }

                BookDto? book = element.Deserialize<BookDto>(options);

                if (book != null)
                {
                    switch (Validate(book))
                    {
                        case ParseOk ok:
                            items.Add(ok.Value);
                            break;
                        case ParseFailed failed:
                            errors.Add($"запис {number}: {failed.Reason}");
                            break;
                    }
                }

                number++;
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"Фатальна помилка структури JSON: {ex.Message}");
        }

        return new ImportResult<BookDto>(items, errors);
    }

    private static ParseOutcome Validate(BookDto book)
    {
        return book switch
        {
            { Isbn: null or "" } or { Title: null or "" }
                => new ParseFailed("ISBN або назва порожні"),

            { Year: var y } when y < 1450 || y > DateTime.Now.Year
                => new ParseFailed($"рік '{y}' поза допустимими межами"),

            _ => new ParseOk(book)
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(BookDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}