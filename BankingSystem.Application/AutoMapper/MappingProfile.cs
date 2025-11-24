using AutoMapper;
using BankingSystem.Application.Dtos;
using BankingSystem.Domain.Entities;
using BankingSystem.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingSystem.Application.AutoMapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Account, AccountDto>()
            .ForMember(dest => dest.UserName,
                opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty));

        CreateMap<CreateAccountDto, Account>()
            .ForMember(dest => dest.Balance, opt => opt.MapFrom(src => src.InitialDeposit))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => AccountStatus.Active))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        CreateMap<Transaction, TransactionDto>()
            .ForMember(dest => dest.AccountNumber, opt => opt.Ignore());

        CreateMap<DepositDto, Transaction>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => TransactionType.Deposit))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => TransactionStatus.Completed))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.TransactionReference, opt => opt.Ignore());

        CreateMap<WithdrawalDto, Transaction>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => TransactionType.Withdrawal))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => TransactionStatus.Completed))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.TransactionReference, opt => opt.Ignore());

        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.Ignore());

        CreateMap<RegisterDto, ApplicationUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.EmailConfirmed, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}
