using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Exceptions;

namespace Core.Import;

public sealed class AsyncJsonBookImporter(TimeSpan? delayPerItem = null) : IBookImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async IAsyncEnumerable<BookDto> LoadAsync(
        string path,
        IProgress<int>? progress = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        List<BookDto?> dtos;
        await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
                         FileShare.Read, 4096, useAsync: true))
        {
            dtos = await JsonSerializer.DeserializeAsync<List<BookDto?>>(stream, Options, ct)
                   ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");
        }

        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var dto = dtos[i];
            Validate(dto, i + 1);

            if (delayPerItem is { } delay && delay > TimeSpan.Zero)
                await Task.Delay(delay, ct);

            progress?.Report((i + 1) * 100 / dtos.Count);
            yield return dto!;
        }
    }

    private static void Validate(BookDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній запис.");

        try { BookCopy.FromDto(dto); }
        catch (ArgumentException ex) 
        {
            throw new DomainException($"Запис {number}: {ex.Message}", ex);
        }
    }
}