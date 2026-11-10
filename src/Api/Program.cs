using Api;
using Api.Endpoints;
using Core;
using Core.Abstractions;
using Core.Configuration;
using Core.Services;
using Core.Storage;
using Scalar.AspNetCore;
using Api.Contracts;
using Api.Validation;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "CROSSAPP_");

var options = builder.Configuration.Get<AppOptions>() ?? new AppOptions();
var legacy = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
var storage = new StorageOptions
{
    DataPath = string.IsNullOrWhiteSpace(legacy) ? options.Storage.DataPath : legacy,
    FileName = options.Storage.FileName
};
var dataFile = storage.ResolveFullPath(AppContext.BaseDirectory);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);
builder.Services.AddSingleton<IValidator<SaveBookRequest>, SaveBookRequestValidator>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddSingleton<ILibraryStore>(_ =>
{
    var store = new SynchronizedLibraryStore(new FileLibraryStore(dataFile));
    if (store.List().Count == 0)
        foreach (var b in SampleData.Books()) store.Add(b);
    return store;
});
builder.Services.AddSingleton<LendingService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               
    app.MapScalarApiReference();    
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapBooks();

app.Run();

public partial class Program;      