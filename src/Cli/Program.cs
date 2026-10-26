using Cli.Sinks;
using Core;
using Core.Abstractions;
using Core.Events;
using Core.Reports;
using Core.Services;
using Core.Storage;
using Core.Import;
using Core.Exceptions;
using System.Threading;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--sysinfo"))
{
    EnvironmentReport report = EnvironmentInfo.Collect();
    var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), WriteIndented = true };
    if (args.Contains("--json")) Console.WriteLine(JsonSerializer.Serialize(report, options));
    else
    {
        Console.WriteLine(report.BuildNote);
        Console.WriteLine("LibraryApp – практикум з крос-платформного програмування");
        Console.WriteLine("Студент: Цебровський Ігор, група FeI-31c");
        Console.WriteLine(new string('-', 52));
        Console.WriteLine($"ОС : {report.OsDescription}");
        Console.WriteLine($"Runtime : {report.FrameworkDescription}");
        Console.WriteLine($"Каталог : {report.BaseDirectory}");
        Console.WriteLine(new string('-', 52));
        Console.WriteLine("Предметна область: <Бібліотека> (видання, примірники, читачі, видачі)");
    }
    return 0;
}

ILibraryStore store = StoreFactory.Create(args, AppContext.BaseDirectory);

if (!store.List().Any())
{
    foreach (var b in SampleData.Books()) store.Add(b);
}

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

var sinks = new INotificationSink[] { new ConsoleSink(), new FileSink(logPath) };
var service = new LendingService(store, sinks);

int changes = 0;
service.Changed += (_, _) => changes++;

var items = service.All();
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

switch (command)
{
    case "import":
        Console.WriteLine("=== Асинхронний імпорт каталогу ===");

        var consoleSink = (ConsoleSink)sinks.First(s => s is ConsoleSink);
        consoleSink.IsMuted = true;

        using (var userCts = new CancellationTokenSource())
        using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token))
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                userCts.Cancel();
            };

            var progress = new SyncProgress<int>(percent =>
                Console.Write($"\r[Хід імпорту]: {percent}% завершено..."));

            var importPath = Path.Combine(Environment.CurrentDirectory, "data", "sample.json");

            try
            {
                var importer = new AsyncJsonBookImporter(TimeSpan.FromMilliseconds(100));
                var added = await service.ImportAsync(importer, importPath, progress, linkedCts.Token);
                Console.WriteLine($"\nУспішно імпортовано нових записів: {added}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(timeoutCts.IsCancellationRequested
                    ? "\nІмпорт скасовано: перевищено час очікування (таймаут)."
                    : "\nІмпорт скасовано користувачем (Ctrl+C).");
            }
            catch (DomainException ex)
            {
                Console.WriteLine($"\nДані відхилено: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"\nФайл пошкоджено (JSON): {ex.Message}");
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Console.WriteLine($"\nФайл або папку не знайдено: {importPath}");
            }
        }
        break;

    case "report-top":
        Console.WriteLine("=== Топ-5 найновіших книг (Власний метод-розширення) ===");
        PrintLines(ReportService.TopNewest(items, 5));
        break;

    case "report-old":
        Console.WriteLine("=== Старі книги (до 2005 року) ===");
        PrintLines(ReportService.OldBooks(items, 2005));
        break;

    case "report-groups":
        Console.WriteLine("=== Статистика за авторами (Синтаксис запитів) ===");
        foreach (var g in ReportService.ByAuthor(items))
            Console.WriteLine($" {g.Key,-20} | Позицій: {g.Count,2} | Найстаріша: {g.OldestYear}");
        break;

    case "report-sum":
        var t = ReportService.Totals(items);
        Console.WriteLine("=== Загальні підсумки ===");
        Console.WriteLine($" Всього позицій: {t.Items}, Доступно: {t.Available}");
        Console.WriteLine($" Середній рік: {t.AverageYear:F1}, Найновіша: {t.MaxYear}");
        break;

    case "report-join":
        Console.WriteLine("=== Активні видачі (Join з іншою колекцією) ===");
        var mockLendings = new List<MockLendingRecord> {
            new("C-101", "Іван Франко", 14),
            new("C-116", "Леся Українка", 5)
        };
        var joined = ReportService.GetActiveLendings(items, mockLendings);
        foreach (var l in joined)
            Console.WriteLine($" [{l.BookId}] {l.Title,-25} -> Читач: {l.ReaderName,-15} ({l.DaysBorrowed} днів)");
        break;

    case "demo-deferred":
        Console.WriteLine("=== Демонстрація відкладеного виконання LINQ ===");
        var sourceList = items.ToList();

        var query = sourceList.Where(b => b.Year >= 2017);
        Console.WriteLine($"Сучасних книг (до змін): {query.Count()}");

        sourceList.Add(Core.Domain.BookCopy.Create("C-999", "111", "Нова Сучасна Книга", 2026, "Автор"));

        Console.WriteLine($"Сучасних книг (після змін): {query.Count()}");

        var snapshot = query.ToList();
        sourceList.Clear();
        Console.WriteLine($"У знімку (snapshot) залишилось: {snapshot.Count} (дані врятовані)");
        break;

    case "report-perf":
        RunPerformanceTest();
        break;

    default:
        Console.WriteLine("Команди: import | report-top | report-old | report-groups | report-sum | report-join | demo-deferred | report-perf");
        break;
}

return 0;

static void PrintLines(IReadOnlyList<LibraryReportLine> lines)
{
    foreach (var l in lines)
        Console.WriteLine($" [{l.Id,-6}] {l.Title,-30} | {l.Author,-20} | {l.Year} | {l.Status}");
}

static void RunPerformanceTest()
{
    Console.WriteLine("=== Тест продуктивності LINQ (100 000 записів) ===");

    var random = new Random(42);
    var hugeData = Enumerable.Range(1, 100000)
        .Select(i => Core.Domain.BookCopy.Create($"C-{i:D6}", "111", $"Книга {i}", 1900 + random.Next(125), "Автор"))
        .ToList();

    var sw = new Stopwatch();

    sw.Start();
    var query1 = hugeData
        .Where(b => b.Year > 2000)
        .OrderBy(b => b.Year)
        .Select(b => b.Title)
        .ToList();
    sw.Stop();
    var time1 = sw.ElapsedMilliseconds;

    sw.Restart();
    var query2 = hugeData
        .Where(b => b.Year > 2000)
        .ToList()
        .OrderBy(b => b.Year)
        .Select(b => b.Title)
        .ToList();
    sw.Stop();
    var time2 = sw.ElapsedMilliseconds;

    Console.WriteLine($"1. Без ToList всередині (Відкладене виконання): {time1} мс");
    Console.WriteLine($"2. З проміжним ToList  (Створення копії)      : {time2} мс");
}