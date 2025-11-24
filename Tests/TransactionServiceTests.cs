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

public class TransactionServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<TransactionService>> _loggerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly TransactionService _transactionService;

    public TransactionServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditServiceMock = new Mock<IAuditService>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<TransactionService>>();
        _cacheMock = MockCacheFactory.CreateCacheService();

        _transactionService = new TransactionService(
            _unitOfWorkMock.Object,
            _auditServiceMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _cacheMock.Object);
    }

    [Fact]
    public async Task DepositAsync_ValidDeposit_Success()
    {
        // Arrange
        var depositDto = new DepositDto
        {
            AccountNumber = "1234567890",
            Amount = 500m,
            Description = "Test deposit"
        };

        var account = new Account
        {
            Id = 1,
            AccountNumber = "1234567890",
            UserId = "user-1",
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        var transactionDto = new TransactionDto
        {
            TransactionReference = "TXN123",
            Amount = 500m
        };

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Account, bool>>>()))
            .ReturnsAsync(account);
        mockAccountRepo.Setup(r => r.Update(It.IsAny<Account>()));

        var mockTransactionRepo = new Mock<ITransactionRepository>();
        mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(mockTransactionRepo.Object);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
        
        // Setup mapper to return a new transaction object
        _mapperMock.Setup(m => m.Map<Transaction>(It.IsAny<DepositDto>()))
            .Returns(new Transaction 
            { 
                Amount = depositDto.Amount,
                Description = depositDto.Description,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed
            });
        
        _mapperMock.Setup(m => m.Map<TransactionDto>(It.IsAny<Transaction>()))
            .Returns(transactionDto);

        _auditServiceMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _transactionService.DepositAsync(depositDto, "user-1");

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(500m);
        account.Balance.Should().Be(1500m);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        mockAccountRepo.Verify(r => r.Update(account), Times.Once);
        mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_InactiveAccount_ThrowsException()
    {
        // Arrange
        var depositDto = new DepositDto
        {
            AccountNumber = "1234567890",
            Amount = 500m,
            Description = "Test deposit"
        };

        var account = new Account
        {
            AccountNumber = "1234567890",
            UserId = "user-1",
            Status = AccountStatus.Frozen
        };

        _unitOfWorkMock.Setup(u => u.Accounts.FirstOrDefaultAsync(It.IsAny<Expression<Func<Account, bool>>>()))
            .ReturnsAsync(account);

        // Act & Assert
        await FluentActions.Invoking(() => _transactionService.DepositAsync(depositDto, "user-1"))
            .Should().ThrowAsync<AccountInactiveException>();
    }

    [Fact]
    public async Task WithdrawAsync_ValidWithdrawal_Success()
    {
        // Arrange
        var withdrawalDto = new WithdrawalDto
        {
            AccountNumber = "1234567890",
            Amount = 300m,
            Description = "ATM withdrawal"
        };

        var account = new Account
        {
            Id = 1,
            AccountNumber = "1234567890",
            UserId = "user-1",
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        var transactionDto = new TransactionDto
        {
            TransactionReference = "TXN123",
            Amount = 300m
        };

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Account, bool>>>()))
            .ReturnsAsync(account);
        mockAccountRepo.Setup(r => r.Update(It.IsAny<Account>()));

        var mockTransactionRepo = new Mock<ITransactionRepository>();
        mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(mockTransactionRepo.Object);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
        
        _mapperMock.Setup(m => m.Map<Transaction>(It.IsAny<WithdrawalDto>()))
            .Returns(new Transaction
            {
                Amount = withdrawalDto.Amount,
                Description = withdrawalDto.Description,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Completed
            });
        
        _mapperMock.Setup(m => m.Map<TransactionDto>(It.IsAny<Transaction>()))
            .Returns(transactionDto);

        _auditServiceMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _transactionService.WithdrawAsync(withdrawalDto, "user-1");

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(300m);
        account.Balance.Should().Be(700m);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_InsufficientFunds_ThrowsException()
    {
        // Arrange
        var withdrawalDto = new WithdrawalDto
        {
            AccountNumber = "1234567890",
            Amount = 2000m,
            Description = "Large withdrawal"
        };

        var account = new Account
        {
            AccountNumber = "1234567890",
            UserId = "user-1",
            Balance = 1000m,
            Status = AccountStatus.Active
        };

        _unitOfWorkMock.Setup(u => u.Accounts.FirstOrDefaultAsync(It.IsAny<Expression<Func<Account, bool>>>()))
            .ReturnsAsync(account);

        // Act & Assert
        await FluentActions.Invoking(() => _transactionService.WithdrawAsync(withdrawalDto, "user-1"))
            .Should().ThrowAsync<InsufficientFundsException>()
            .WithMessage("Insufficient funds in account 1234567890. Available: $1,000.00, Requested: $2,000.00");
    }

    [Fact]
    public async Task TransferAsync_ValidTransfer_Success()
    {
        // Arrange
        var transferDto = new TransferDto
        {
            FromAccountNumber = "1111111111",
            ToAccountNumber = "2222222222",
            Amount = 200m,
            Description = "Transfer"
        };

        var fromAccount = new Account
        {
            Id = 1,
            AccountNumber = "1111111111",
            UserId = "user-1",
            Balance = 500m,
            Status = AccountStatus.Active
        };

        var toAccount = new Account
        {
            Id = 2,
            AccountNumber = "2222222222",
            UserId = "user-2",
            Balance = 100m,
            Status = AccountStatus.Active
        };

        var debitTransactionDto = new TransactionDto
        {
            TransactionReference = "TXN001",
            Amount = 200m,
            Type = TransactionType.TransferOut
        };

        var creditTransactionDto = new TransactionDto
        {
            TransactionReference = "TXN002",
            Amount = 200m,
            Type = TransactionType.TransferIn
        };

        var mockAccountRepo = new Mock<IAccountRepository>();
        mockAccountRepo.SetupSequence(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Account, bool>>>()))
            .ReturnsAsync(fromAccount)
            .ReturnsAsync(toAccount);
        mockAccountRepo.Setup(r => r.Update(It.IsAny<Account>()));

        var mockTransactionRepo = new Mock<ITransactionRepository>();
        mockTransactionRepo.Setup(r => r.AddAsync(It.IsAny<Transaction>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
        _unitOfWorkMock.Setup(u => u.Transactions).Returns(mockTransactionRepo.Object);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);

        _mapperMock.Setup(m => m.Map<TransactionDto>(It.Is<Transaction>(t => t.Type == TransactionType.TransferOut)))
            .Returns(debitTransactionDto);
        _mapperMock.Setup(m => m.Map<TransactionDto>(It.Is<Transaction>(t => t.Type == TransactionType.TransferIn)))
            .Returns(creditTransactionDto);

        _auditServiceMock.Setup(a => a.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var (debit, credit) = await _transactionService.TransferAsync(transferDto, "user-1");

        // Assert
        debit.Amount.Should().Be(200m);
        credit.Amount.Should().Be(200m);
        fromAccount.Balance.Should().Be(300m);
        toAccount.Balance.Should().Be(300m);

        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitTransactionAsync(), Times.Once);
        mockAccountRepo.Verify(r => r.Update(It.IsAny<Account>()), Times.Exactly(2));
        mockTransactionRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Exactly(2));
    }

    [Fact]
    public async Task TransferAsync_SameAccount_ThrowsException()
    {
        // Arrange
        var transferDto = new TransferDto
        {
            FromAccountNumber = "1111111111",
            ToAccountNumber = "1111111111",
            Amount = 100m,
            Description = "Invalid transfer"
        };

        var account = new Account
        {
            AccountNumber = "1111111111",
            UserId = "user-1",
            Balance = 500m,
            Status = AccountStatus.Active
        };

        _unitOfWorkMock.SetupSequence(u => u.Accounts.FirstOrDefaultAsync(It.IsAny<Expression<Func<Account, bool>>>()))
            .ReturnsAsync(account)
            .ReturnsAsync(account);

        // Act & Assert
        await FluentActions.Invoking(() => _transactionService.TransferAsync(transferDto, "user-1"))
            .Should().ThrowAsync<InvalidTransactionException>()
            .WithMessage("Cannot transfer to the same account");
    }

    [Fact]
    public async Task GetAccountTransactionsAsync_ReturnsFilteredTransactions()
    {
        // Arrange
        var accountNumber = "1234567890";
        var fromDate = DateTime.UtcNow.AddDays(-30);
        var toDate = DateTime.UtcNow;

        var transactions = new List<Transaction>
        {
            new() { Id = 1, AccountId = 1, Amount = 100m, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = 2, AccountId = 1, Amount = 200m, CreatedAt = DateTime.UtcNow.AddDays(-10) }
        };

        var transactionDtos = new List<TransactionDto>
        {
            new() { Id = 1, Amount = 100m, AccountNumber = accountNumber },
            new() { Id = 2, Amount = 200m, AccountNumber = accountNumber }
        };

        var mockTransactionRepo = new Mock<ITransactionRepository>();
        mockTransactionRepo.Setup(r => r.GetTransactionsByAccountNumberAsync(accountNumber, fromDate, toDate))
            .ReturnsAsync(transactions);

        _unitOfWorkMock.Setup(u => u.Transactions).Returns(mockTransactionRepo.Object);
        
        _mapperMock.Setup(m => m.Map<IEnumerable<TransactionDto>>(It.IsAny<IEnumerable<Transaction>>()))
            .Returns(transactionDtos);

        // Act
        var result = await _transactionService.GetAccountTransactionsAsync(accountNumber, fromDate, toDate);

        // Assert
        result.Should().HaveCount(2);
        result.All(t => t.AccountNumber == accountNumber).Should().BeTrue();
    }
}