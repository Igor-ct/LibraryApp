using System;
using System.IO;
using Core.Abstractions;
using Core.Events;

namespace Cli.Sinks;

public class ConsoleSink : INotificationSink
{
    public bool IsMuted { get; set; } = false;

    public void Notify(LibraryChangedEventArgs args)
    {
        if (IsMuted) return;
        Console.WriteLine($"[ConsoleSink] {args.At:HH:mm:ss} {args.Kind,-15} {args.CopyId}");
    }
}


public class FileSink(string logPath) : INotificationSink
{
    public void Notify(LibraryChangedEventArgs args)
    {
        var line = $"[FileSink] {args.At:HH:mm:ss} {args.Kind,-14} {args.CopyId}";
        File.AppendAllText(logPath, line + Environment.NewLine);
    }
}