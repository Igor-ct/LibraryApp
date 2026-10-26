using System.Runtime.CompilerServices;
using Core.Dto;

namespace Core.Abstractions;

public interface IBookImporter
{
    IAsyncEnumerable<BookDto> LoadAsync(
        string path,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}