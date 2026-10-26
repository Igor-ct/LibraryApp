using System;
using Core.Dto;
using Core.Exceptions;

namespace Core.Domain;

public sealed class Reader
{
    public string Id { get; }
    public string TicketNumber { get; }
    public string FullName { get; }
    public int RegistrationYear { get; }

    public int ActiveLoansCount { get; private set; }

    private Reader(string id, string ticketNumber, string fullName, int registrationYear, int activeLoansCount)
    {
        Id = id;
        TicketNumber = ticketNumber;
        FullName = fullName;
        RegistrationYear = registrationYear;
        ActiveLoansCount = activeLoansCount;
    }

    public static Reader Create(string id, string ticketNumber, string fullName, int registrationYear, int activeLoansCount = 0)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(ticketNumber))
            throw new ArgumentException("Номер квитка обов'язковий", nameof(ticketNumber));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Ім'я обов'язкове", nameof(fullName));
        if (registrationYear < 1900 || registrationYear > DateTime.Now.Year)
            throw new ArgumentOutOfRangeException(nameof(registrationYear), "Некоректний рік реєстрації");
        if (activeLoansCount < 0)
            throw new ArgumentOutOfRangeException(nameof(activeLoansCount), "Кількість не може бути від'ємною");

        return new Reader(id.Trim(), ticketNumber.Trim(), fullName.Trim(), registrationYear, activeLoansCount);
    }

    public Loan BorrowBook(string loanId, BookCopy copy, DateTime issuedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);

        if (ActiveLoansCount >= 5)
            throw new DomainException($"Читач {FullName} вже має максимальну кількість відкритих видач ({5}).");

        var loan = Loan.Open(loanId, copy.Id, Id, issuedOn); 
        copy.Issue();                                        
        ActiveLoansCount++;

        return loan;
    }

    public void ReturnBook(BookCopy copy, Loan loan, DateTime returnedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);
        ArgumentNullException.ThrowIfNull(loan);

        if (loan.ReaderId != Id || loan.CopyId != copy.Id)
            throw new DomainException($"Видача {loan.Id} не належить читачу {FullName} або іншому примірнику.");
        if (!copy.IsIssued)
            throw new DomainException($"Примірник {copy.Id} не видавався");

        loan.Close(returnedOn);   
        copy.Return();
        ActiveLoansCount--;
    }

    public ReaderDto ToDto() => new(Id, TicketNumber, FullName, RegistrationYear);

    public static Reader FromDto(ReaderDto dto, int activeLoansCount = 0) =>
        Create(dto.Id, dto.TicketNumber, dto.FullName, dto.RegistrationYear, activeLoansCount);
}