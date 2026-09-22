using Core;
using Core.Dto;
using Core.Import;
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

return 0;