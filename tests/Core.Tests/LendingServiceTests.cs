using Core.Events;
using Core.Exceptions;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class LendingServiceTests
{
    [Fact]
    public void AddBook_RaisesChangedExactlyOnce()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var events = new List<LibraryChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.AddBook("ISBN-123", "Тестова книга", 2024, "Автор");

        Assert.Single(events);
        Assert.Equal(LibraryChangeKind.BookAdded, events[0].Kind);
        Assert.Single(service.All());
    }

    [Fact]
    public void AddBook_NotifiesAllSinks()
    {
        var sink = new SpySink();
        var service = new LendingService(new InMemoryLibraryStore(), new[] { sink });

        service.AddBook("ISBN-1", "Книга", 2020, "А");

        Assert.Single(sink.Received);
    }

    [Fact]
    public void AddBook_EmptyTitle_ThrowsArgumentException_AndStoresNothing()
    {
        var service = new LendingService(new InMemoryLibraryStore());

        Assert.Throws<ArgumentException>(() => service.AddBook("ISBN-1", " ", 2020));
        Assert.Empty(service.All());
    }

    [Fact]
    public void IssueCopy_MarksCopyAndRaisesEvent()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var copy = service.AddBook("ISBN-1", "Книга", 2020, "А");
        var events = new List<LibraryChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.IssueCopy(copy.Id);

        Assert.True(service.Find(copy.Id)!.IsIssued);
        Assert.Single(events);
        Assert.Equal(LibraryChangeKind.CopyIssued, events[0].Kind);
    }

    [Fact]
    public void IssueCopy_Twice_ThrowsDomainException_AndRaisesNoSecondEvent()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var copy = service.AddBook("ISBN-1", "Книга", 2020, "А");
        service.IssueCopy(copy.Id);
        var events = new List<LibraryChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Assert.Throws<DomainException>(() => service.IssueCopy(copy.Id));
        Assert.Empty(events);
    }

    [Fact]
    public void IssueCopy_UnknownId_ThrowsDomainException() =>
        Assert.Throws<DomainException>(() =>
            new LendingService(new InMemoryLibraryStore()).IssueCopy("nope"));

    [Fact]
    public void ReturnCopy_AfterIssue_RaisesCopyReturned()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var copy = service.AddBook("ISBN-1", "Книга", 2020, "А");
        service.IssueCopy(copy.Id);
        var events = new List<LibraryChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.ReturnCopy(copy.Id);

        Assert.False(service.Find(copy.Id)!.IsIssued);
        Assert.Single(events);
        Assert.Equal(LibraryChangeKind.CopyReturned, events[0].Kind);
    }

    [Fact]
    public void ReturnCopy_NotIssued_ThrowsDomainException()
    {
        var service = new LendingService(new InMemoryLibraryStore());
        var copy = service.AddBook("ISBN-1", "Книга", 2020, "А");

        Assert.Throws<DomainException>(() => service.ReturnCopy(copy.Id));
    }

    [Fact]
    public void ReturnCopy_UnknownId_ThrowsDomainException() =>
        Assert.Throws<DomainException>(() =>
            new LendingService(new InMemoryLibraryStore()).ReturnCopy("nope"));
}