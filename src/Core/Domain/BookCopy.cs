using System;
using Core.Dto;

namespace Core.Domain;

public sealed class BookCopy
{
    public string Id { get; }
    public string Isbn { get; }
    public string Title { get; }
    public int Year { get; }
    public string? Author { get; }
    public bool IsIssued { get; private set; }

    private BookCopy(string id, string isbn, string title, int year, string? author, bool isIssued)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        Year = year;
        Author = author;
        IsIssued = isIssued;
    }

    public static BookCopy Create(string id, string isbn, string title, int year, string? author = null, bool isIssued = false)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN не може бути порожнім", nameof(isbn));

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Назва не може бути порожньою", nameof(title));

        if (year < 1000 || year > DateTime.Now.Year)
            throw new ArgumentOutOfRangeException(nameof(year), year, "Некоректний рік видання");

        return new BookCopy(id.Trim(), isbn.Trim().ToUpperInvariant(), title.Trim(), year, author?.Trim(), isIssued);
    }

    public void Issue()
    {
        if (IsIssued)
            throw new InvalidOperationException($"Примірник {Id} вже виданий, повторна видача неможлива");
        IsIssued = true;
    }

    public void Return()
    {
        if (!IsIssued)
            throw new InvalidOperationException($"Примірник {Id} не видавався");
        IsIssued = false;
    }


    public BookDto ToDto() => new(Id, Isbn, Title, Year, Author);

    public static BookCopy FromDto(BookDto dto, bool isIssued = false) =>
        Create(dto.Id, dto.Isbn, dto.Title, dto.Year, dto.Author, isIssued);
}