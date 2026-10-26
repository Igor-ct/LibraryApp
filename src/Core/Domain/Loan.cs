using Core.Dto;
using Core.Exceptions;
using System;

namespace Core.Domain;

public sealed class Loan
{
    public string Id { get; }
    public string CopyId { get; }
    public string ReaderId { get; }
    public DateTime IssuedOn { get; }
    public DateTime? ReturnedOn { get; private set; }
    public LoanStatus Status { get; private set; }

    private Loan(string id, string copyId, string readerId, DateTime issuedOn, DateTime? returnedOn, LoanStatus status)
    {
        Id = id;
        CopyId = copyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = returnedOn;
        Status = status;
    }

    public static Loan Open(string id, string copyId, string readerId, DateTime issuedOn)
    {
        ValidateCommon(id, copyId, readerId);
        return new Loan(id.Trim(), copyId.Trim(), readerId.Trim(), issuedOn, null, LoanStatus.Open);
    }

    public void ChangeStatus(LoanStatus newStatus)
    {
        Status = (Status, newStatus) switch
        {
            (LoanStatus.Open, LoanStatus.Closed) => newStatus,
            (LoanStatus.Open, LoanStatus.Overdue) => newStatus,
            (LoanStatus.Overdue, LoanStatus.Closed) => newStatus,
            _ => throw new DomainException($"Неможливий перехід видачі {Id} зі стану {Status} у {newStatus}")
        };
    }

    public void Close(DateTime returnedOn)
    {
        if (returnedOn < IssuedOn)
            throw new ArgumentOutOfRangeException(nameof(returnedOn), "Дата повернення не може бути раніше дати видачі");

        ChangeStatus(LoanStatus.Closed); 
        ReturnedOn = returnedOn;
    }

    private static void ValidateCommon(string id, string copyId, string readerId)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Id видачі обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(copyId)) throw new ArgumentException("Id примірника обов'язковий", nameof(copyId));
        if (string.IsNullOrWhiteSpace(readerId)) throw new ArgumentException("Id читача обов'язковий", nameof(readerId));
    }
}