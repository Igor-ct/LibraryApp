using Api.Contracts;
using Api.Validation;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class BookEndpoints
{
    public static void MapBooks(this WebApplication app)
    {
        var group = app.MapGroup("/api/books").WithTags("Books");

        group.MapGet("/", Results<Ok<PagedResponse<BookResponse>>, ValidationProblem> (
            string? author, string? search, int? page, int? pageSize, ILibraryStore store) =>
        {
            var p = page ?? 1;
            var size = pageSize ?? 20;

            var errors = new Dictionary<string, string[]>();
            if (p < 1) errors["page"] = ["page має бути не менше 1."];
            if (size is < 1 or > 100) errors["pageSize"] = ["pageSize має бути від 1 до 100."];
            if (errors.Count > 0) return TypedResults.ValidationProblem(errors);

            var query = store.List().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(author))
                query = query.Where(b => b.Author?.Contains(author, StringComparison.OrdinalIgnoreCase) ?? false);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b =>
                    b.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || (b.Author?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || b.Isbn.Contains(search, StringComparison.OrdinalIgnoreCase));

            var filtered = query.OrderBy(b => b.Id, StringComparer.Ordinal).ToList();

            var items = filtered.Skip((p - 1) * size).Take(size).Select(b => b.ToResponse()).ToList();
            return TypedResults.Ok(new PagedResponse<BookResponse>(items, filtered.Count));
        });

        group.MapGet("/{id}", Results<Ok<BookResponse>, NotFound> (string id, ILibraryStore store) =>
            store.GetById(id) is { } book
                ? TypedResults.Ok(book.ToResponse())
                : TypedResults.NotFound());

        group.MapPost("/", Created<BookResponse> (SaveBookRequest request, LendingService service) =>
        {
            var created = service.AddBook(request.Isbn, request.Title, request.Year, request.Author);
            return TypedResults.Created($"/api/books/{created.Id}", created.ToResponse());
        })
        .AddEndpointFilter<ValidationFilter<SaveBookRequest>>()
        .ProducesValidationProblem();

        group.MapPut("/{id}", Results<Ok<BookResponse>, NotFound> (string id, SaveBookRequest request, ILibraryStore store) =>
        {
            if (store.GetById(id) is not { } existing) return TypedResults.NotFound();

            var replacement = BookCopy.Create(id, request.Isbn, request.Title, request.Year, request.Author, existing.IsIssued);
            store.Update(replacement);
            return TypedResults.Ok(replacement.ToResponse());
        })
        .AddEndpointFilter<ValidationFilter<SaveBookRequest>>()
        .ProducesValidationProblem();

        group.MapDelete("/{id}", Results<NoContent, NotFound> (string id, ILibraryStore store) =>
            store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound());

        group.MapPost("/{id}/issue", Results<Ok<BookResponse>, NotFound> (string id, LendingService service) =>
        {
            if (service.Find(id) is null) return TypedResults.NotFound();
            service.IssueCopy(id);
            return TypedResults.Ok(service.Find(id)!.ToResponse());
        }).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id}/return", Results<Ok<BookResponse>, NotFound> (string id, LendingService service) =>
        {
            if (service.Find(id) is null) return TypedResults.NotFound();
            service.ReturnCopy(id);
            return TypedResults.Ok(service.Find(id)!.ToResponse());
        }).ProducesProblem(StatusCodes.Status409Conflict);
    }
}