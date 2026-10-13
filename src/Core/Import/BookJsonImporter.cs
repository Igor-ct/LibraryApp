using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class BookJsonImporter
{
    public static ImportResult<BookDto> Load(string path, Action<string>? onSkip = null)
    {
        var items = new List<BookDto>();
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path);

            using JsonDocument doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                string msg = "Фатальна помилка: JSON має бути масивом об'єктів";
                errors.Add(msg);
                onSkip?.Invoke(msg); 
                return new ImportResult<BookDto>(items, errors);
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            int number = 1;

            foreach (JsonElement element in doc.RootElement.EnumerateArray())
            {
                int propCount = element.EnumerateObject().Count();
                if (propCount > 5)
                {
                    string msg = $"запис {number}: занадто багато параметрів, отримав {propCount}";
                    errors.Add(msg);
                    onSkip?.Invoke(msg);
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
                            string msg = $"запис {number}: {failed.Reason}";
                            errors.Add(msg);
                            onSkip?.Invoke(msg);
                            break;
                    }
                }

                number++;
            }
        }
        catch (JsonException ex)
        {
            string msg = $"Фатальна помилка структури JSON: {ex.Message}";
            errors.Add(msg);
            onSkip?.Invoke(msg); 
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