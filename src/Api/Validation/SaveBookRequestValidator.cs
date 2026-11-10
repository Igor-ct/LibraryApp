using Api.Contracts;
using FluentValidation;

namespace Api.Validation;

public sealed class SaveBookRequestValidator : AbstractValidator<SaveBookRequest>
{
    public SaveBookRequestValidator()
    {
        RuleFor(x => x.Isbn)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("ISBN обов'язковий.")
            .MaximumLength(20).WithMessage("ISBN: до 20 символів.");

        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Назва обов'язкова.")
            .MaximumLength(200).WithMessage("Назва: до 200 символів.");

        RuleFor(x => x.Author)
            .MaximumLength(100).WithMessage("Автор: до 100 символів.");

        RuleFor(x => x.Year)
            .Must(y => y >= 1000 && y <= DateTime.Now.Year)
            .WithMessage(_ => $"Рік має бути від 1000 до {DateTime.Now.Year}.");
    }
}