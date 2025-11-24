using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MyBankingSolution.Extensions;

/// <summary>
/// Extension methods for controllers
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Get all model state errors as a dictionary
    /// </summary>
    public static Dictionary<string, string[]> GetModelStateErrors(this ModelStateDictionary modelState)
    {
        return modelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
            );
    }

    /// <summary>
    /// Get all model state errors as a flat list
    /// </summary>
    public static List<string> GetModelStateErrorsList(this ModelStateDictionary modelState)
    {
        return modelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }

    /// <summary>
    /// Check if ModelState is valid and return errors if not
    /// </summary>
    public static (bool isValid, Dictionary<string, string[]> errors) ValidateModel(this ModelStateDictionary modelState)
    {
        if (modelState.IsValid)
            return (true, new Dictionary<string, string[]>());

        return (false, modelState.GetModelStateErrors());
    }
}
