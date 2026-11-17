using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Api.Tests;

public sealed class BooksApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"api_{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> _factory;
    private readonly WebApplicationFactory<Program> _app;
    private readonly HttpClient _client;

    public BooksApiTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__AppDb", $"Data Source={_dbPath}");
        _factory = factory;
        _app = factory.WithWebHostBuilder(_ => { });
        _client = _app.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _app.Dispose();
        Environment.SetEnvironmentVariable("ConnectionStrings__AppDb", null);
        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_dbPath) + "*"))
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }

    private static SaveBookRequest Valid(string title = "Кобзар", string isbn = "978-966-03-0001-1") =>
        new(isbn, title, "Тарас Шевченко", 1840);

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
            await _client.PostAsJsonAsync("/api/books", Valid($"Книга {i}", $"978-0-{i:D3}"));

        var response = await _client.GetFromJsonAsync<PagedResponse<BookResponse>>("/api/books?page=1&pageSize=2");

        Assert.Equal(2, response!.Items.Count);
        Assert.Equal(5, response.Total);
    }

    [Fact]
    public async Task List_InvalidPageSize_Returns400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/books?pageSize=0")).StatusCode);

    [Fact]
    public async Task Data_Survives_ApplicationRestart()
    {
        string id;
        using (var first = _factory.WithWebHostBuilder(_ => { }))
        {
            var created = await first.CreateClient().PostAsJsonAsync("/api/books", Valid());
            id = (await created.Content.ReadFromJsonAsync<BookResponse>())!.Id;
        }   

        using var second = _factory.WithWebHostBuilder(_ => { });
        var response = await second.CreateClient().GetAsync($"/api/books/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_SameEditionTwice_CreatesTwoCopies()
    {
        var a = await (await _client.PostAsJsonAsync("/api/books", Valid())).Content.ReadFromJsonAsync<BookResponse>();
        var b = await (await _client.PostAsJsonAsync("/api/books", Valid())).Content.ReadFromJsonAsync<BookResponse>();

        Assert.NotEqual(a!.Id, b!.Id);
        Assert.Equal(2, (await _client.GetFromJsonAsync<PagedResponse<BookResponse>>("/api/books"))!.Total);
    }

    [Fact]
    public async Task Post_SameIsbnDifferentEdition_Returns409()
    {
        await _client.PostAsJsonAsync("/api/books", Valid("Кобзар"));

        var response = await _client.PostAsJsonAsync("/api/books", Valid("Інша назва"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

}
