using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;
using BankingSystem.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Tests;

public class AccountRepositoryTests : IDisposable
{
    private readonly BankingSystem.Infrastructure.Data.BankDbContext _context;
    private readonly AccountRepository _repository;

    public AccountRepositoryTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<BankingSystem.Infrastructure.Data.BankDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        _context = new BankingSystem.Infrastructure.Data.BankDbContext(options);
        _repository = new AccountRepository(_context);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 1, 1),
            IsActive = true
        };

        var accounts = new List<Account>
        {
            new()
            {
                Id = 1,
                AccountNumber = "1234567890",
                UserId = "user-1",
                User = user,
                AccountType = AccountType.Savings,
                Balance = 1000m,
                Status = AccountStatus.Active,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                AccountNumber = "0987654321",
                UserId = "user-1",
                User = user,
                AccountType = AccountType.Checking,
                Balance = 2500m,
                Status = AccountStatus.Active,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                AccountNumber = "1111111111",
                UserId = "user-1",
                User = user,
                AccountType = AccountType.Savings,
                Balance = 500m,
                Status = AccountStatus.Frozen,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow
            }
        };

        _context.Users.Add(user);
        _context.Accounts.AddRange(accounts);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByAccountNumberAsync_ExistingAccount_ReturnsAccount()
    {
        // Act
        var result = await _repository.GetByAccountNumberAsync("1234567890");

        // Assert
        result.Should().NotBeNull();
        result!.AccountNumber.Should().Be("1234567890");
        result.Balance.Should().Be(1000m);
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByAccountNumberAsync_NonExistingAccount_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByAccountNumberAsync("9999999999");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveAccountsByUserIdAsync_ReturnsOnlyActiveAccounts()
    {
        // Act
        var result = await _repository.GetActiveAccountsByUserIdAsync("user-1");

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.Status == AccountStatus.Active).Should().BeTrue();
        result.All(a => a.UserId == "user-1").Should().BeTrue();
    }

    [Fact]
    public async Task GetAccountsByTypeAsync_Savings_ReturnsSavingsAccounts()
    {
        // Act
        var result = await _repository.GetAccountsByTypeAsync(AccountType.Savings);

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.AccountType == AccountType.Savings).Should().BeTrue();
    }

    [Fact]
    public async Task GetAccountsWithMinimumBalanceAsync_ReturnsAccountsAboveThreshold()
    {
        // Act
        var result = await _repository.GetAccountsWithMinimumBalanceAsync(1000m);

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.Balance >= 1000m).Should().BeTrue();
        result.All(a => a.Status == AccountStatus.Active).Should().BeTrue();
    }

    [Fact]
    public async Task GetTotalBalanceByUserIdAsync_CalculatesCorrectTotal()
    {
        // Act
        var result = await _repository.GetTotalBalanceByUserIdAsync("user-1");

        // Assert
        result.Should().Be(3500m); // 1000 + 2500 (active accounts only)
    }

    [Fact]
    public async Task GetInactiveAccountsAsync_ReturnsNonActiveAccounts()
    {
        // Act
        var result = await _repository.GetInactiveAccountsAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Status.Should().Be(AccountStatus.Frozen);
    }

    [Fact]
    public async Task GetAccountsQuery_ReturnsIQueryable()
    {
        // Act
        var query = _repository.GetAccountsQuery();
        var result = await query.Where(a => a.Balance > 1000m).ToListAsync();

        // Assert
        query.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Balance.Should().Be(2500m);
    }

    [Fact]
    public async Task GetActiveAccountsQuery_OnlyActiveAccounts()
    {
        // Act
        var query = _repository.GetActiveAccountsQuery();
        var result = await query.ToListAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(a => a.Status == AccountStatus.Active).Should().BeTrue();
    }

    [Fact]
    public async Task GetAccountsByUserIdQuery_FiltersCorrectly()
    {
        // Act
        var query = _repository.GetAccountsByUserIdQuery("user-1");
        var result = await query.ToListAsync();

        // Assert
        result.Should().HaveCount(3);
        result.All(a => a.UserId == "user-1").Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_AddsNewAccount()
    {
        // Arrange
        var newAccount = new Account
        {
            AccountNumber = "5555555555",
            UserId = "user-1",
            AccountType = AccountType.BusinessChecking,
            Balance = 10000m,
            Status = AccountStatus.Active,
            Currency = "USD"
        };

        // Act
        await _repository.AddAsync(newAccount);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByAccountNumberAsync("5555555555");
        result.Should().NotBeNull();
        result!.Balance.Should().Be(10000m);
        result.AccountType.Should().Be(AccountType.BusinessChecking);
    }

    [Fact]
    public async Task Update_UpdatesAccountBalance()
    {
        // Arrange
        var account = await _repository.GetByAccountNumberAsync("1234567890");
        account!.Balance = 1500m;

        // Act
        _repository.Update(account);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _repository.GetByAccountNumberAsync("1234567890");
        updated!.Balance.Should().Be(1500m);
    }

    [Fact]
    public async Task Remove_RemovesAccount()
    {
        // Arrange
        var account = await _repository.GetByAccountNumberAsync("1234567890");

        // Act
        _repository.Remove(account!);
        await _context.SaveChangesAsync();

        // Assert
        var result = await _repository.GetByAccountNumberAsync("1234567890");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWithTransactionsAsync_LoadsTransactions()
    {
        // Arrange
        var transaction = new Transaction
        {
            AccountId = 1,
            TransactionReference = "TXN001",
            Type = TransactionType.Deposit,
            Amount = 100m,
            BalanceBefore = 1000m,
            BalanceAfter = 1100m,
            Status = TransactionStatus.Completed,
            Description = "Test deposit",
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetWithTransactionsAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Transactions.Should().HaveCount(1);
        result.Transactions.First().TransactionReference.Should().Be("TXN001");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
