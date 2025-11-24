using BankingSystem.Application.Dtos;
using BankingSystem.Application.Validators;
using BankingSystem.Domain.Entities.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests;

public class DepositValidatorTests
{
    private readonly DepositValidator _validator;

    public DepositValidatorTests()
    {
        _validator = new DepositValidator();
    }

    [Fact]
    public void Validate_ValidDeposit_PassesValidation()
    {
        // Arrange
        var dto = new DepositDto
        {
            AccountNumber = "1234567890",
            Amount = 100m,
            Description = "Valid deposit"
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
        var dto = new DepositDto
        {
            AccountNumber = "",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(d => d.AccountNumber);
    }

    [Fact]
    public void Validate_InvalidAccountNumberLength_FailsValidation()
    {
        // Arrange
        var dto = new DepositDto
        {
            AccountNumber = "123",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(d => d.AccountNumber);
    }

    [Fact]
    public void Validate_ZeroAmount_FailsValidation()
    {
        // Arrange
        var dto = new DepositDto
        {
            AccountNumber = "1234567890",
            Amount = 0m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(d => d.Amount)
            .WithErrorMessage("Amount must be greater than 0");
    }

    [Fact]
    public void Validate_NegativeAmount_FailsValidation()
    {
        // Arrange
        var dto = new DepositDto
        {
            AccountNumber = "1234567890",
            Amount = -100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(d => d.Amount);
    }

    [Fact]
    public void Validate_ExcessiveAmount_FailsValidation()
    {
        // Arrange
        var dto = new DepositDto
        {
            AccountNumber = "1234567890",
            Amount = 1000001m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(d => d.Amount)
            .WithErrorMessage("Amount cannot exceed 1000000");
    }

    [Fact]
    public void Validate_EmptyDescription_FailsValidation()
    {
        // Arrange
        var dto = new DepositDto
        {
            AccountNumber = "1234567890",
            Amount = 100m,
            Description = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(d => d.Description);
    }

    [Fact]
    public void Validate_TooLongDescription_FailsValidation()
    {
        // Arrange
        var dto = new DepositDto
        {
            AccountNumber = "1234567890",
            Amount = 100m,
            Description = new string('a', 501)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(d => d.Description);
    }
}
