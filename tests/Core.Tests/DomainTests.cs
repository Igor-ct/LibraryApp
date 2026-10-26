using Core.Domain;
using Core.Exceptions;
using Xunit;

namespace Core.Tests;

public class DomainTests
{
    [Fact]
    public void BorrowBook_OverLimit_ThrowsDomainException_AndKeepsState()
    {
        var reader = Reader.Create("R1", "T1", "Іван", 2020, activeLoansCount: 5);
        var copy = BookCopy.Create("C-1", "1", "Книга", 2020);

        Assert.Throws<DomainException>(() => reader.BorrowBook("L1", copy, DateTime.Now));
        Assert.False(copy.IsIssued);
        Assert.Equal(5, reader.ActiveLoansCount);
    }

    [Fact]
    public void BorrowBook_InvalidLoanId_ThrowsArgumentException_AndKeepsState()
    {
        var reader = Reader.Create("R1", "T1", "Іван", 2020);
        var copy = BookCopy.Create("C-1", "1", "Книга", 2020);

        Assert.Throws<ArgumentException>(() => reader.BorrowBook(" ", copy, DateTime.Now));
        Assert.False(copy.IsIssued);
        Assert.Equal(0, reader.ActiveLoansCount);
    }

    [Fact]
    public void ReturnBook_NotIssuedCopy_ThrowsDomainException_AndKeepsLoanOpen()
    {
        var reader = Reader.Create("R1", "T1", "Іван", 2020);
        var copy = BookCopy.Create("C-1", "1", "Книга", 2020);
        var loan = Loan.Open("L1", "C-1", "R1", DateTime.Now);

        Assert.Throws<DomainException>(() => reader.ReturnBook(copy, loan, DateTime.Now));
        Assert.Equal(LoanStatus.Open, loan.Status);
    }

    [Fact]
    public void Loan_CloseTwice_ThrowsDomainException()
    {
        var loan = Loan.Open("L1", "C-1", "R1", DateTime.Now.AddDays(-1));
        loan.Close(DateTime.Now);

        Assert.Throws<DomainException>(() => loan.Close(DateTime.Now));
    }

    [Fact]
    public void Loan_CloseBeforeIssue_ThrowsArgumentOutOfRange()
    {
        var loan = Loan.Open("L1", "C-1", "R1", DateTime.Now);

        Assert.Throws<ArgumentOutOfRangeException>(() => loan.Close(DateTime.Now.AddDays(-1)));
    }
}