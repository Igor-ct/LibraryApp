using Api;
using Api.Contracts;
using Api.Endpoints;
using Api.Validation;
using Core.Abstractions;
using Core.Services;
using FluentValidation;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables(prefix: "CROSSAPP_");

var raw = builder.Configuration.GetConnectionString("AppDb") ?? "Data Source=data/app.db";
var csb = new SqliteConnectionStringBuilder(raw);
if (!Path.IsPathRooted(csb.DataSource))
{
    var full = Path.Combine(builder.Environment.ContentRootPath, csb.DataSource);
    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
    csb.DataSource = full;
}

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddSingleton<IValidator<SaveBookRequest>, SaveBookRequestValidator>();

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(csb.ConnectionString));
builder.Services.AddScoped<ILibraryStore, EfLibraryStore>();
builder.Services.AddScoped<LendingService>();   

var app = builder.Build();

if (app.Environment.IsDevelopment())           
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

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