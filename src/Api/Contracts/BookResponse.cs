using Core.Domain;

namespace Api.Contracts;

public sealed record BookResponse(string Id, string Isbn, string Title, string? Author, int Year, bool IsIssued);

public static class BookMapping
{
    public static BookResponse ToResponse(this BookCopy b) =>
        new(b.Id, b.Isbn, b.Title, b.Author, b.Year, b.IsIssued);
}