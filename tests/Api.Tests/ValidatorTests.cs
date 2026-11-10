using Api.Contracts;
using Api.Validation;
using Xunit;

namespace Api.Tests;

public class ValidatorTests
{
    private readonly SaveBookRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_Passes() =>
        Assert.True(_validator.Validate(new SaveBookRequest("978-1", "Кобзар", "Шевченко", 1840)).IsValid);

    [Fact]
    public void AuthorIsOptional() =>
        Assert.True(_validator.Validate(new SaveBookRequest("978-1", "Кобзар", null, 1840)).IsValid);

    [Theory]
    [InlineData("", "Назва", 2000, "Isbn")]
    [InlineData("   ", "Назва", 2000, "Isbn")]
    [InlineData("1", "", 2000, "Title")]
    [InlineData("1", "Назва", 999, "Year")]
    [InlineData("1", "Назва", 3000, "Year")]
    public void InvalidField_ReportsThatField(string isbn, string title, int year, string expectedKey)
    {
        var result = _validator.Validate(new SaveBookRequest(isbn, title, null, year));

        Assert.False(result.IsValid);
        Assert.Contains(result.ToDictionary().Keys, k => k == expectedKey);
    }

    [Fact]
    public void TooLongTitle_Fails() =>
        Assert.False(_validator.Validate(new SaveBookRequest("1", new string('x', 201), null, 2000)).IsValid);

    [Fact]
    public void MultipleErrors_AreAllReported()
    {
        var errors = _validator.Validate(new SaveBookRequest("", "", null, 3000)).ToDictionary();

        Assert.Equal(3, errors.Count);
    }
}