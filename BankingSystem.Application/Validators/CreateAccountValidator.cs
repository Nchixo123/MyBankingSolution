using BankingSystem.Application.Dtos;
using BankingSystem.Domain.Entities.Enums;
using FluentValidation;

namespace BankingSystem.Application.Validators;

public class CreateAccountValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.AccountType)
            .IsInEnum().WithMessage("Invalid account type");

        RuleFor(x => x.InitialDeposit)
            .GreaterThanOrEqualTo(0).WithMessage("Initial deposit cannot be negative")
            .PrecisionScale(18, 2, false).WithMessage("Amount cannot have more than 2 decimal places");
    }
}
