using BankingSystem.Application.Dtos;
using BankingSystem.Application.Validators;
using BankingSystem.Domain.Entities.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests;

public class CreateAccountValidatorTests
{
    private readonly CreateAccountValidator _validator;

    public CreateAccountValidatorTests()
    {
        _validator = new CreateAccountValidator();
    }

    [Fact]
    public void Validate_ValidCreateAccount_PassesValidation()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            UserId = "user-123",
            AccountType = AccountType.Savings,
            InitialDeposit = 1000m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyUserId_FailsValidation()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            UserId = "",
            AccountType = AccountType.Savings,
            InitialDeposit = 1000m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public void Validate_NullUserId_FailsValidation()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            UserId = null!,
            AccountType = AccountType.Savings,
            InitialDeposit = 1000m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public void Validate_InvalidAccountType_FailsValidation()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            UserId = "user-123",
            AccountType = (AccountType)999,
            InitialDeposit = 1000m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.AccountType);
    }

    [Fact]
    public void Validate_NegativeInitialDeposit_FailsValidation()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            UserId = "user-123",
            AccountType = AccountType.Savings,
            InitialDeposit = -100m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.InitialDeposit)
            .WithErrorMessage("Initial deposit must be 0 or greater");
    }

    [Fact]
    public void Validate_ZeroInitialDeposit_PassesValidation()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            UserId = "user-123",
            AccountType = AccountType.Savings,
            InitialDeposit = 0m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.InitialDeposit);
    }

    [Fact]
    public void Validate_ExcessiveInitialDeposit_FailsValidation()
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            UserId = "user-123",
            AccountType = AccountType.Savings,
            InitialDeposit = 10000001m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.InitialDeposit)
            .WithErrorMessage("Initial deposit cannot exceed 10000000");
    }

    [Theory]
    [InlineData(AccountType.Savings)]
    [InlineData(AccountType.Checking)]
    [InlineData(AccountType.BusinessChecking)]
    [InlineData(AccountType.MoneyMarket)]
    public void Validate_AllValidAccountTypes_PassValidation(AccountType accountType)
    {
        // Arrange
        var dto = new CreateAccountDto
        {
            UserId = "user-123",
            AccountType = accountType,
            InitialDeposit = 1000m
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.AccountType);
    }
}
