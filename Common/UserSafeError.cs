using Microsoft.EntityFrameworkCore;
using WMS.Models;

namespace WMS.Common;

public static class UserSafeError
{
    public const string GenericMessage = "Thao tác chưa hoàn tất. Vui lòng thử lại hoặc liên hệ quản trị viên nếu lỗi tiếp diễn.";

    public static string From(Exception ex, string? fallback = null)
    {
        if (IsBusinessSafe(ex))
            return string.IsNullOrWhiteSpace(ex.Message) ? fallback ?? GenericMessage : ex.Message;

        return fallback ?? GenericMessage;
    }

    public static string WithPrefix(Exception ex, string prefix, string? fallback = null)
    {
        var message = From(ex, fallback);
        var generic = fallback ?? GenericMessage;
        return string.Equals(message, generic, StringComparison.Ordinal)
            ? message
            : $"{prefix}: {message}";
    }

    public static bool IsBusinessSafe(Exception ex)
    {
        if (ex is DbUpdateException or IOException)
            return false;

        return ex is BusinessRuleException
            or WarehouseLockedException
            or ConcurrencyException
            or SodViolationException
            or UnauthorizedAccessException
            or KeyNotFoundException
            or ArgumentException
            or InvalidOperationException;
    }
}
