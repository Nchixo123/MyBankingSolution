using BankingSystem.Application.Dtos;
using BankingSystem.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Tests;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator;

    public RegisterValidatorTests()
    {
        _validator = new RegisterValidator();
    }

    [Fact]
    public void Validate_ValidRegistration_PassesValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = "123 Main St"
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
        var dto = new RegisterDto
        {
            Email = "",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Email);
    }

    [Fact]
    public void Validate_InvalidEmailFormat_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "invalid-email",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Email)
            .WithErrorMessage("Invalid email format");
    }

    [Fact]
    public void Validate_ShortPassword_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@1",
            ConfirmPassword = "Test@1",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Password);
    }

    [Fact]
    public void Validate_PasswordMismatch_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Different@123",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.ConfirmPassword)
            .WithErrorMessage("Passwords do not match");
    }

    [Fact]
    public void Validate_EmptyFirstName_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.FirstName);
    }

    [Fact]
    public void Validate_EmptyLastName_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.LastName);
    }

    [Fact]
    public void Validate_TooLongFirstName_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = new string('a', 101),
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.FirstName);
    }

    [Fact]
    public void Validate_FutureDateOfBirth_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.DateOfBirth)
            .WithErrorMessage("Date of birth cannot be in the future");
    }

    [Fact]
    public void Validate_UnderageDateOfBirth_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = DateTime.UtcNow.AddYears(-10)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.DateOfBirth)
            .WithErrorMessage("Must be at least 18 years old");
    }

    [Fact]
    public void Validate_TooLongAddress_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1),
            Address = new string('a', 501)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Address);
    }
}
