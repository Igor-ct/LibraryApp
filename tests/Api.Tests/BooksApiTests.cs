using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Core.Abstractions;
using Core.Storage;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Api.Tests;

public sealed class BooksApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BooksApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<ILibraryStore>();
            s.AddSingleton<ILibraryStore>(new SynchronizedLibraryStore(new InMemoryLibraryStore()));
        })).CreateClient();
    }

    private static SaveBookRequest Valid(string title = "Кобзар") =>
        new("978-966-03-0001-1", title, "Тарас Шевченко", 1840);

    [Fact]
    public async Task Post_Returns201_WithLocation_AndGetByLocationReturns200()
    {
        var post = await _client.PostAsJsonAsync("/api/books", Valid());

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.NotNull(post.Headers.Location);

        var get = await _client.GetAsync(post.Headers.Location);
        var book = await get.Content.ReadFromJsonAsync<BookResponse>();

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Кобзар", book!.Title);
        Assert.False(book.IsIssued);
    }

    [Fact]
    public async Task FullCycle_Put_Delete_ThenGetReturns404()
    {
        var created = await (await _client.PostAsJsonAsync("/api/books", Valid()))
            .Content.ReadFromJsonAsync<BookResponse>();

        var put = await _client.PutAsJsonAsync($"/api/books/{created!.Id}", Valid("Нова назва"));
        var updated = await put.Content.ReadFromJsonAsync<BookResponse>();
        var delete = await _client.DeleteAsync($"/api/books/{created.Id}");
        var after = await _client.GetAsync($"/api/books/{created.Id}");

        Assert.Equal("Нова назва", updated!.Title);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }

    [Fact]
    public async Task Post_InvalidBody_Returns400_WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/books", new SaveBookRequest("", "X", null, 3000));
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Isbn", problem!.Errors.Keys);
        Assert.Contains("Year", problem.Errors.Keys);
    }

    [Fact]
    public async Task Post_WrongFieldType_Returns400_NotServerError()
    {
        var content = new StringContent("""{"isbn":"1","title":"X","year":"abc"}""",
            System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/books", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Issue_Twice_Returns409_WithProblemDetails()
    {
        var created = await (await _client.PostAsJsonAsync("/api/books", Valid()))
            .Content.ReadFromJsonAsync<BookResponse>();

        var first = await _client.PostAsync($"/api/books/{created!.Id}/issue", null);
        var second = await _client.PostAsync($"/api/books/{created.Id}/issue", null);
        var body = await second.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.DoesNotContain("Exception", body);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404() =>
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/books/nope")).StatusCode);

    [Fact]
    public async Task List_Paging_ReturnsPageAndTotal()
    {
        for (var i = 0; i < 5; i++)
            await _client.PostAsJsonAsync("/api/books", Valid($"Книга {i}"));

        var response = await _client.GetFromJsonAsync<PagedResponse<BookResponse>>("/api/books?page=1&pageSize=2");

        Assert.Equal(2, response!.Items.Count);
        Assert.Equal(5, response.Total);
    }

    [Fact]
    public async Task List_InvalidPageSize_Returns400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/books?pageSize=0")).StatusCode);


}
