using BankingSystem.Application.Dtos;
using FluentValidation;

namespace BankingSystem.Application.Validators;

public class TransferValidator : AbstractValidator<TransferDto>
{
    public TransferValidator()
    {
        RuleFor(x => x.FromAccountNumber)
            .NotEmpty().WithMessage("Source account number is required")
            .Length(10).WithMessage("Account number must be 10 digits");
        RuleFor(x => x.ToAccountNumber)
            .NotEmpty().WithMessage("Destination account number is required")
            .Length(10).WithMessage("Account number must be 10 digits")
            .NotEqual(x => x.FromAccountNumber).WithMessage("Cannot transfer to the same account");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(100000).WithMessage("Transfer cannot exceed $100,000 per transaction")
            .PrecisionScale(18, 2, false).WithMessage("Amount cannot have more than 2 decimal places");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .MinimumLength(3).WithMessage("Description must be at least 3 characters");
    }
}
