using BankingSystem.Domain.Entities.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyBankingSolution.Extensions;

/// <summary>
/// Helper methods for creating ViewBag data
/// </summary>
public static class ViewBagHelpers
{
    /// <summary>
    /// Get SelectList for AccountType enum
    /// </summary>
    public static SelectList GetAccountTypesSelectList(AccountType? selectedValue = null)
    {
        return selectedValue.HasValue
            ? new SelectList(Enum.GetValues(typeof(AccountType)), selectedValue)
            : new SelectList(Enum.GetValues(typeof(AccountType)));
    }

    /// <summary>
    /// Get SelectList for TransactionType enum
    /// </summary>
    public static SelectList GetTransactionTypesSelectList(TransactionType? selectedValue = null)
    {
        return selectedValue.HasValue
            ? new SelectList(Enum.GetValues(typeof(TransactionType)), selectedValue)
            : new SelectList(Enum.GetValues(typeof(TransactionType)));
    }

    /// <summary>
    /// Get SelectList for TransactionStatus enum
    /// </summary>
    public static SelectList GetTransactionStatusSelectList(TransactionStatus? selectedValue = null)
    {
        return selectedValue.HasValue
            ? new SelectList(Enum.GetValues(typeof(TransactionStatus)), selectedValue)
            : new SelectList(Enum.GetValues(typeof(TransactionStatus)));
    }

    /// <summary>
    /// Get SelectList from any enum
    /// </summary>
    public static SelectList GetEnumSelectList<TEnum>(TEnum? selectedValue = null) where TEnum : struct, Enum
    {
        return selectedValue.HasValue
            ? new SelectList(Enum.GetValues(typeof(TEnum)), selectedValue)
            : new SelectList(Enum.GetValues(typeof(TEnum)));
    }
}
