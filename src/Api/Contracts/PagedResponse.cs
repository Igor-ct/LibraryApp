namespace Api.Contracts;

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total);