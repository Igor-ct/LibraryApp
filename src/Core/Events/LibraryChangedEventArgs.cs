using System;

namespace Core.Events;

public sealed class LibraryChangedEventArgs(string copyId, LibraryChangeKind kind) : EventArgs
{
    public string CopyId { get; } = copyId;
    public LibraryChangeKind Kind { get; } = kind;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}