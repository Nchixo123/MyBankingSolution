using AutoMapper;
using BankingSystem.Application.Caching;
using BankingSystem.Application.Dtos;
using BankingSystem.Application.Exceptions;
using BankingSystem.Application.Services.Interfaces;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;
using BankingSystem.Domain.Interfaces;
using BankingSystem.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Tests;

public class AccountServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<AccountService>>();
        _cacheMock = MockCacheFactory.CreateCacheService();

        _accountService = new AccountService(
            _unitOfWorkMock.Object,
            _auditServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task CreateAccountAsync_ValidRequest_CreatesAccount()
    {
        // Arrange
        var createDto = new CreateAccountDto
        {
            UserId = "user-1",
            AccountType = AccountType.Savings,
            InitialDeposit = 1000m
        };

        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "testuser",
            Email = "test@example.com",
            IsActive = true
        };

        var account = new Account
        {
            Id = 1,
            AccountNumber = "1234567890",
            UserId = "user-1",
            AccountType = AccountType.Savings,
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        var accountDto = new AccountDto
        {
            Id = 1,
            AccountNumber = "1234567890",
            Balance = 1000m,
            AccountType = AccountType.Savings,
            Status = AccountStatus.Active
        };

        var mockUserRepo = new Mock<IRepository<ApplicationUser>>();
        mockUserRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(user);

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Account, bool>>>()))
            .ReturnsAsync(false);
        mockAccountRepo.Setup(r => r.AddAsync(It.IsAny<Account>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Repository<ApplicationUser>()).Returns(mockUserRepo.Object);
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<Account>(It.IsAny<CreateAccountDto>()))
            .Returns(account);
        _mapperMock.Setup(m => m.Map<AccountDto>(It.IsAny<Account>()))
            .Returns(accountDto);

        _auditServiceMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _accountService.CreateAccountAsync(createDto, "admin-1");

        // Assert
        result.Should().NotBeNull();
        result.AccountNumber.Should().Be("1234567890");
        result.Balance.Should().Be(1000m);
        
        mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_InactiveUser_ThrowsException()
    {
        // Arrange
        var createDto = new CreateAccountDto
        {
            UserId = "user-1",
            AccountType = AccountType.Savings,
            InitialDeposit = 1000m
        };

        var user = new ApplicationUser
        {
            Id = "user-1",
            IsActive = false
        };

        var mockUserRepo = new Mock<IRepository<ApplicationUser>>();
        mockUserRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(user);

        _unitOfWorkMock.Setup(u => u.Repository<ApplicationUser>()).Returns(mockUserRepo.Object);

        // Act & Assert
        await FluentActions.Invoking(() => _accountService.CreateAccountAsync(createDto, "admin-1"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User is not active");
    }

    [Fact]
    public async Task GetAccountByNumberAsync_ExistingAccount_ReturnsAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = 1,
            AccountNumber = "1234567890",
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        var accountDto = new AccountDto
        {
            Id = 1,
            AccountNumber = "1234567890",
            Balance = 1000m
        };

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.GetAccountWithUserAsync("1234567890"))
            .ReturnsAsync(account);

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        
        _mapperMock.Setup(m => m.Map<AccountDto>(It.IsAny<Account>()))
            .Returns(accountDto);

        // Act
        var result = await _accountService.GetAccountByNumberAsync("1234567890");

        // Assert
        result.Should().NotBeNull();
        result!.AccountNumber.Should().Be("1234567890");
    }

    [Fact]
    public async Task GetAccountByNumberAsync_NonExistingAccount_ThrowsException()
    {
        // Arrange
        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.GetAccountWithUserAsync("9999999999"))
            .ReturnsAsync((Account?)null);

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);

        // Act & Assert
        await FluentActions.Invoking(() => _accountService.GetAccountByNumberAsync("9999999999"))
            .Should().ThrowAsync<AccountNotFoundException>()
            .WithMessage("Account with number 9999999999 was not found");
    }

    [Fact]
    public async Task GetUserAccountsAsync_ReturnsUserAccounts()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new() { Id = 1, AccountNumber = "1111111111", UserId = "user-1", Balance = 1000m },
            new() { Id = 2, AccountNumber = "2222222222", UserId = "user-1", Balance = 2000m }
        };

        var accountDtos = new List<AccountDto>
        {
            new() { Id = 1, AccountNumber = "1111111111", Balance = 1000m },
            new() { Id = 2, AccountNumber = "2222222222", Balance = 2000m }
        };

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.GetUserAccountsWithUserAsync("user-1"))
            .ReturnsAsync(accounts);

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        
        _mapperMock.Setup(m => m.Map<IEnumerable<AccountDto>>(It.IsAny<IEnumerable<Account>>()))
            .Returns(accountDtos);

        // Act
        var result = await _accountService.GetUserAccountsAsync("user-1");

        // Assert
        result.Should().HaveCount(2);
        result.Sum(a => a.Balance).Should().Be(3000m);
    }

    [Fact]
    public async Task UpdateAccountStatusAsync_ValidRequest_UpdatesStatus()
    {
        // Arrange
        var account = new Account
        {
            Id = 1,
            AccountNumber = "1234567890",
            Status = AccountStatus.Active,
            Balance = 1000m
        };

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(account);
        mockAccountRepo.Setup(r => r.Update(It.IsAny<Account>()));

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _auditServiceMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _accountService.UpdateAccountStatusAsync(1, AccountStatus.Frozen, "admin-1");

        // Assert
        result.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.Frozen);
        mockAccountRepo.Verify(r => r.Update(account), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAccountsAsync_ReturnsAllAccounts()
    {
        // Arrange
        var accounts = new List<Account>
        {
            new() { Id = 1, AccountNumber = "1111111111", Balance = 1000m },
            new() { Id = 2, AccountNumber = "2222222222", Balance = 2000m },
            new() { Id = 3, AccountNumber = "3333333333", Balance = 3000m }
        };

        var accountDtos = new List<AccountDto>
        {
            new() { Id = 1, AccountNumber = "1111111111", Balance = 1000m },
            new() { Id = 2, AccountNumber = "2222222222", Balance = 2000m },
            new() { Id = 3, AccountNumber = "3333333333", Balance = 3000m }
        };

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.GetAllAccountsWithUserAsync())
            .ReturnsAsync(accounts);

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        
        _mapperMock.Setup(m => m.Map<IEnumerable<AccountDto>>(It.IsAny<IEnumerable<Account>>()))
            .Returns(accountDtos);

        // Act
        var result = await _accountService.GetAllAccountsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Sum(a => a.Balance).Should().Be(6000m);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ExistingAccount_ReturnsAccount()
    {
        // Arrange
        var account = new Account
        {
            Id = 1,
            AccountNumber = "1234567890",
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        var accountDto = new AccountDto
        {
            Id = 1,
            AccountNumber = "1234567890",
            Balance = 1000m
        };

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.GetAccountByIdWithUserAsync(1))
            .ReturnsAsync(account);

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        
        _mapperMock.Setup(m => m.Map<AccountDto>(It.IsAny<Account>()))
            .Returns(accountDto);

        // Act
        var result = await _accountService.GetAccountByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.AccountNumber.Should().Be("1234567890");
    }
}
