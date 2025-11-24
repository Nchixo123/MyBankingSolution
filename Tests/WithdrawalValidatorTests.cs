using BankingSystem.Application.Dtos;
using BankingSystem.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests;

public class WithdrawalValidatorTests
{
    private readonly WithdrawalValidator _validator;

    public WithdrawalValidatorTests()
    {
        _validator = new WithdrawalValidator();
    }

    [Fact]
    public void Validate_ValidWithdrawal_PassesValidation()
    {
        // Arrange
        var dto = new WithdrawalDto
        {
            AccountNumber = "1234567890",
            Amount = 100m,
            Description = "Valid withdrawal"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyAccountNumber_FailsValidation()
    {
        // Arrange
        var dto = new WithdrawalDto
        {
            AccountNumber = "",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(w => w.AccountNumber);
    }

    [Fact]
    public void Validate_InvalidAccountNumberLength_FailsValidation()
    {
        // Arrange
        var dto = new WithdrawalDto
        {
            AccountNumber = "123",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(w => w.AccountNumber);
    }

    [Fact]
    public void Validate_ZeroAmount_FailsValidation()
    {
        // Arrange
        var dto = new WithdrawalDto
        {
            AccountNumber = "1234567890",
            Amount = 0m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(w => w.Amount)
            .WithErrorMessage("Amount must be greater than 0");
    }

    [Fact]
    public void Validate_NegativeAmount_FailsValidation()
    {
        // Arrange
        var dto = new WithdrawalDto
        {
            AccountNumber = "1234567890",
            Amount = -100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(w => w.Amount);
    }

    [Fact]
    public void Validate_ExcessiveAmount_FailsValidation()
    {
        // Arrange
        var dto = new WithdrawalDto
        {
            AccountNumber = "1234567890",
            Amount = 100001m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(w => w.Amount)
            .WithErrorMessage("Single withdrawal cannot exceed 100000");
    }

    [Fact]
    public void Validate_EmptyDescription_FailsValidation()
    {
        // Arrange
        var dto = new WithdrawalDto
        {
            AccountNumber = "1234567890",
            Amount = 100m,
            Description = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(w => w.Description);
    }

    [Fact]
    public void Validate_TooLongDescription_FailsValidation()
    {
        // Arrange
        var dto = new WithdrawalDto
        {
            AccountNumber = "1234567890",
            Amount = 100m,
            Description = new string('a', 501)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(w => w.Description);
    }
}
