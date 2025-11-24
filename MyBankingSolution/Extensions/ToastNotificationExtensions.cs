using Microsoft.AspNetCore.Mvc;

namespace MyBankingSolution.Extensions;

/// <summary>
/// Extension methods for displaying toast notifications in controllers
/// </summary>
public static class ToastNotificationExtensions
{
    private const string SuccessKey = "ToastSuccess";
    private const string ErrorKey = "ToastError";
    private const string WarningKey = "ToastWarning";
    private const string InfoKey = "ToastInfo";

    /// <summary>
    /// Display a success toast notification
    /// </summary>
    public static void AddSuccessToast(this Controller controller, string message)
    {
        controller.TempData[SuccessKey] = message;
    }

    /// <summary>
    /// Display an error toast notification
    /// </summary>
    public static void AddErrorToast(this Controller controller, string message)
    {
        controller.TempData[ErrorKey] = message;
    }

    /// <summary>
    /// Display a warning toast notification
    /// </summary>
    public static void AddWarningToast(this Controller controller, string message)
    {
        controller.TempData[WarningKey] = message;
    }

    /// <summary>
    /// Display an info toast notification
    /// </summary>
    public static void AddInfoToast(this Controller controller, string message)
    {
        controller.TempData[InfoKey] = message;
    }
}
