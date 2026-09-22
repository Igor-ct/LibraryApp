using Core.Dto;

namespace Core.Import;

public static class MixedCsvImporter
{
    public static ImportResult<object> Load(string path)
    {
        var items = new List<object>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue;

            string[] parts = line.Split(';', StringSplitOptions.TrimEntries);

            ParseOutcome outcome = parts switch
            {
                ["B", "", _, _, _] or ["B", _, "", _, _]
                    => new ParseFailed("ISBN або назва порожні"),

                ["B", _, _, _, var year] when !int.TryParse(year, out int y) || y < 1450 || y > DateTime.Now.Year
                    => new ParseFailed($"рік книги '{year}' поза допустимими межами"),

                ["B", var id, var isbn, var title, var year]
                    => new ParseOkBook(new BookDto(id, isbn, title, int.Parse(year))),

                ["B", ..]
                    => new ParseFailed($"очікую 5 колонок для книги, отримав {parts.Length}"),

                ["R", "", _, _, _] or ["R", _, "", _, _]
                    => new ParseFailed("Номер квитка або ПІБ порожні"),

                ["R", _, _, _, var year] when !int.TryParse(year, out int y) || y < 1900 || y > DateTime.Now.Year
                    => new ParseFailed($"рік реєстрації '{year}' поза допустимими межами"),

                ["R", var id, var ticket, var name, var year]
                    => new ParseOkReader(new ReaderDto(id, ticket, name, int.Parse(year))),

                ["R", ..]
                    => new ParseFailed($"очікую 5 колонок для читача, отримав {parts.Length}"),

                [var prefix, ..] when prefix != "B" && prefix != "R"
                    => new ParseFailed($"Невідомий префікс: {prefix}"),

                _ => new ParseFailed($"некоректна кількість колонок: {parts.Length}")
            };

            switch (outcome)
            {
                case ParseOkBook bookResult:
                    items.Add(bookResult.Value);
                    break;
                case ParseOkReader readerResult:
                    items.Add(readerResult.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<object>(items, errors);
    }

    private abstract record ParseOutcome;
    private sealed record ParseOkBook(BookDto Value) : ParseOutcome;
    private sealed record ParseOkReader(ReaderDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}