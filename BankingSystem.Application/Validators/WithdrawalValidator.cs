using BankingSystem.Application.Dtos;
using FluentValidation;

namespace BankingSystem.Application.Validators;

public class WithdrawalValidator : AbstractValidator<WithdrawalDto>
{
    public WithdrawalValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required")
            .Length(10).WithMessage("Account number must be 10 digits");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(100000).WithMessage("Withdrawal cannot exceed $100,000 per transaction")
            .PrecisionScale(18, 2, false).WithMessage("Amount cannot have more than 2 decimal places");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .MinimumLength(3).WithMessage("Description must be at least 3 characters");
    }
}
