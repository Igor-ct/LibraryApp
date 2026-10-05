using Core;
using Core.Abstractions;
using Core.Storage;
using Core.Services;
using System;
using System.Linq;
using System.IO;
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

ILibraryStore store = StoreFactory.Create(args, AppContext.BaseDirectory);
var service = new LendingService(store);

Console.WriteLine($"=== Активне сховище: {store.GetType().Name} ===\n");

Console.WriteLine("=== Сценарій 1: Успіх (Додавання та видача) ===");

var book1 = service.AddBook("978-0132350884", "Clean Code", 2008, "Robert C. Martin");
var book2 = service.AddBook("978-0201835953", "The Mythical Man-Month", 1995, "Frederick P. Brooks");

Console.WriteLine($"Додано книгу: {book1.Id} [{book1.Isbn}] {book1.Title}");
Console.WriteLine($"Додано книгу: {book2.Id} [{book2.Isbn}] {book2.Title}");

service.IssueCopy(book1.Id);
Console.WriteLine($"Книгу {book1.Id} успішно видано.");

Console.WriteLine("\n=== Сценарій 2: Пошук виданих книг (Func<BookCopy, bool>) ===");

var issuedBooks = store.Find(b => b.IsIssued);
foreach (var b in issuedBooks)
{
    Console.WriteLine($" Знайдено видану книгу: [{b.Id}] {b.Title}");
}

Console.WriteLine("\n=== Поточний стан каталогу ===");
foreach (var p in service.All())
{
    string status = p.IsIssued ? "Видано" : "В наявності";
    Console.WriteLine($" [{p.Id,-6}] {p.Isbn,-15} {p.Title,-25} | {status}");
}

Console.WriteLine("\n=== Сценарій 4: Відмова ===");
try
{
    service.IssueCopy("C-9999");
    Console.WriteLine(" Помилка: виняток НЕ спрацював!");
}
catch (Exception ex)
{
    Console.WriteLine($" Перехоплено помилку ({ex.GetType().Name}): {ex.Message}");
}

try
{
    service.IssueCopy(book1.Id);
    Console.WriteLine(" Помилка: виняток НЕ спрацював!");
}
catch (Exception ex)
{
    Console.WriteLine($" Перехоплено помилку ({ex.GetType().Name}): {ex.Message}");
}

return 0;