using BankingSystem.Application.Dtos;
using BankingSystem.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator;

    public LoginValidatorTests()
    {
        _validator = new LoginValidator();
    }

    [Fact]
    public void Validate_ValidLogin_PassesValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_FailsValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "",
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(l => l.Email);
    }

    [Fact]
    public void Validate_InvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "invalid-email",
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(l => l.Email)
            .WithErrorMessage("Invalid email format");
    }

    [Fact]
    public void Validate_EmptyPassword_FailsValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(l => l.Password);
    }

    [Fact]
    public void Validate_NullEmail_FailsValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = null!,
            Password = "Test@123"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(l => l.Email);
    }

    [Fact]
    public void Validate_NullPassword_FailsValidation()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@example.com",
            Password = null!
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(l => l.Password);
    }
}
