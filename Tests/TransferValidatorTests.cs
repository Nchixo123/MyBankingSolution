using BankingSystem.Application.Dtos;
using BankingSystem.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests;

public class TransferValidatorTests
{
    private readonly TransferValidator _validator;

    public TransferValidatorTests()
    {
        _validator = new TransferValidator();
    }

    [Fact]
    public void Validate_ValidTransfer_PassesValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "0987654321",
            Amount = 100m,
            Description = "Valid transfer"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyFromAccountNumber_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "",
            ToAccountNumber = "0987654321",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.FromAccountNumber);
    }

    [Fact]
    public void Validate_EmptyToAccountNumber_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.ToAccountNumber);
    }

    [Fact]
    public void Validate_SameAccountNumbers_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "1234567890",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.ToAccountNumber)
            .WithErrorMessage("Cannot transfer to the same account");
    }

    [Fact]
    public void Validate_InvalidFromAccountLength_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "123",
            ToAccountNumber = "0987654321",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.FromAccountNumber);
    }

    [Fact]
    public void Validate_InvalidToAccountLength_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "123",
            Amount = 100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.ToAccountNumber);
    }

    [Fact]
    public void Validate_ZeroAmount_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "0987654321",
            Amount = 0m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.Amount)
            .WithErrorMessage("Amount must be greater than 0");
    }

    [Fact]
    public void Validate_NegativeAmount_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "0987654321",
            Amount = -100m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.Amount);
    }

    [Fact]
    public void Validate_ExcessiveAmount_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "0987654321",
            Amount = 500001m,
            Description = "Test"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.Amount)
            .WithErrorMessage("Single transfer cannot exceed 500000");
    }

    [Fact]
    public void Validate_EmptyDescription_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "0987654321",
            Amount = 100m,
            Description = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.Description);
    }

    [Fact]
    public void Validate_TooLongDescription_FailsValidation()
    {
        // Arrange
        var dto = new TransferDto
        {
            FromAccountNumber = "1234567890",
            ToAccountNumber = "0987654321",
            Amount = 100m,
            Description = new string('a', 501)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(t => t.Description);
    }
}
