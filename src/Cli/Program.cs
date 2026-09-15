using System.Runtime.InteropServices;
using System.Text.Json;
using Core;

EnvironmentReport report = EnvironmentInfo.Collect();
Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--json"))
{
    Console.WriteLine(JsonSerializer.Serialize(report));
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
