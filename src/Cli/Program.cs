using Core;
using Core.Dto;
using Core.Domain;
using Core.Import;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--sysinfo"))
{
    EnvironmentReport report = EnvironmentInfo.Collect();
    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        WriteIndented = true
    };

    if (args.Contains("--json"))
    {
        Console.WriteLine(JsonSerializer.Serialize(report, options));
    }
    else
    {
        Console.WriteLine(report.BuildNote);
        Console.WriteLine("LibraryApp – практикум з крос-платформного програмування");
        Console.WriteLine("Студент: Цебровський Ігор, група FeI-31c");
        Console.WriteLine(new string('-', 52));
        Console.WriteLine($"ОС : {report.OsDescription}");
        Console.WriteLine($"Runtime : {report.FrameworkDescription}");
        Console.WriteLine($"Архітектура : {report.ProcessArchitecture}");
        Console.WriteLine($"RID (визначено): {report.DetectedRid}");
        Console.WriteLine($"RID (від .NET) : {report.ReportedRid}");
        Console.WriteLine($"Каталог : {report.BaseDirectory}");
        Console.WriteLine(new string('-', 52));
        Console.WriteLine("Предметна область: <Бібліотека> (видання, примірники, читачі, видачі)");
    }

    return 0;
}


Console.WriteLine("\n=== Сценарій 1: Успіх ===");
BookCopy copy = BookCopy.Create("C-001", "978-0131103627", "The C Programming Language", 1988, "Brian W. Kernighan");
Reader reader = Reader.Create("R-001", "T-1234", "Іван Франко", 2020, activeLoansCount: 0);

Console.WriteLine($"Створено примірник: {copy.Id} [{copy.Isbn}] «{copy.Title}». Видано: {copy.IsIssued}");

Loan loan = reader.BorrowBook("L-001", copy, DateTime.Now.AddDays(-5));
Console.WriteLine($"Книгу видано. Стан примірника: {copy.IsIssued}. Видача: {loan.Id} (Статус: {loan.Status}) для читача {reader.FullName}");

reader.ReturnBook(copy, loan, DateTime.Now);
Console.WriteLine($"Книгу повернуто. Стан видачі: {loan.Status}");
Console.WriteLine();


Console.WriteLine("=== Сценарій 2: Порушення інваріантів ===");
TryDo("Порожній ISBN", () => BookCopy.Create("C-002", "   ", "Clean Code", 2008));
TryDo("Некоректний рік", () => BookCopy.Create("C-003", "123", "Майбутня книга", 2050));
TryDo("Повторна видача", () =>
{
    var badCopy = BookCopy.Create("C-004", "123-456", "1984", 1949);
    badCopy.Issue();
    badCopy.Issue();
});
TryDo("Дата повернення в минулому", () =>
{
    var badLoan = Loan.Open("L-002", "C-001", "R-001", DateTime.Now);
    badLoan.Close(DateTime.Now.AddDays(-1));
});
TryDo("Ліміт видач (2 сутності)", () =>
{
    var overloadedReader = Reader.Create("R-002", "T-9999", "Тарас Шевченко", 2015, activeLoansCount: 5);
    overloadedReader.BorrowBook("L-003", BookCopy.Create("C-005", "111-222", "Кобзар", 1840), DateTime.Now);
});
TryDo("Порушення машини станів", () =>
{
    var closedLoan = Loan.Open("L-004", "C-001", "R-001", DateTime.Now.AddDays(-2));
    closedLoan.Close(DateTime.Now);
    closedLoan.ChangeStatus(LoanStatus.Open);
});
Console.WriteLine(new string('-', 70));


string path = args.FirstOrDefault(a => !a.StartsWith("--")) ?? Path.Combine("data", "sample.csv");
if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();
ImportResult<object> result = (extension, args.Contains("--mixed")) switch
{
    (".json", false) => ToObjectResult(BookJsonImporter.Load(path)),
    (".csv", true) => MixedCsvImporter.Load(path),
    (".csv", false) => ToObjectResult(BookCsvImporter.Load(path)),
    _ => throw new NotSupportedException($"Формат {extension} не підтримується")
};

ImportResult<object> ToObjectResult(ImportResult<BookDto> res) =>
    new ImportResult<object>(res.Items.Cast<object>().ToList(), res.Errors);

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
Console.WriteLine(new string('-', 70));
Console.WriteLine(result.Statistics);
Console.WriteLine(new string('-', 70));

foreach (object item in result.Items.Take(5))
{
    if (item is BookDto b)
    {
        Console.WriteLine($" [Книга]  {b.Id,-6} {b.Isbn,-14} {b.Title,-26} {b.Year,5}");
    }
    else if (item is ReaderDto r)
    {
        Console.WriteLine($" [Читач]  {r.Id,-6} {r.TicketNumber,-14} {r.FullName,-26} {r.RegistrationYear,5}");
    }
}

if (result.Errors.Count > 0)
{
    Console.WriteLine($"\nПропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
    {
        Console.WriteLine($" ! {e}");
    }
}

Console.WriteLine($"\n=== Сценарій 3: Валідація завантажених DTO через Домен ===");

var bookDtos = result.Items.OfType<BookDto>().ToList();
var typedDtoResult = new ImportResult<BookDto>(bookDtos, result.Errors);

var domainResult = ImportMapper.MapBooksToDomain(typedDtoResult);

Console.WriteLine($"Успішно створено доменних сутностей: {domainResult.Items.Count}");
if (domainResult.Errors.Count > 0)
{
    Console.WriteLine($"Знайдено помилки валідації ({domainResult.Errors.Count}):");
    foreach (var err in domainResult.Errors)
    {
        Console.WriteLine($" ! {err}");
    }
}

return 0;

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
    }
}