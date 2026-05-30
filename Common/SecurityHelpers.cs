using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WMS.Common;

/// <summary>
/// Shared security utilities — eliminates duplicate code across controllers.
/// </summary>
public static class SecurityHelpers
{
    /// <summary>
    /// Validates password strength: min 8 chars, upper + lower + digit + symbol.
    /// </summary>
    public static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8) return false;
        bool hasLower = password.Any(char.IsLower);
        bool hasUpper = password.Any(char.IsUpper);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));
        return hasLower && hasUpper && hasDigit && hasSymbol;
    }

    /// <summary>
    /// Basic email format validation (RFC 5322 simplified).
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sanitizes user-facing error messages — strips potentially sensitive info.
    /// </summary>
    public static string SanitizeErrorMessage(string rawMessage, bool isDevelopment = false)
    {
        if (isDevelopment) return rawMessage;

        // Strip connection strings, API keys, stack traces
        if (rawMessage.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
            rawMessage.Contains("Bearer ", StringComparison.OrdinalIgnoreCase) ||
            rawMessage.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            rawMessage.Contains("StackTrace", StringComparison.OrdinalIgnoreCase))
        {
            return "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
        }
        return rawMessage;
    }

    /// <summary>
    /// Whitelist of valid WMS table names — used to prevent SQL injection in raw SQL operations.
    /// </summary>
    public static readonly HashSet<string> ValidTableNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "UnitsOfMeasure", "UnitConversions", "PackagingUnits",
        "ItemCategories", "Partners", "Warehouses", "Zones", "Locations",
        "Items", "ItemLocations",
        "Vouchers", "VoucherDetails",
        "StockAlerts", "StockSnapshots", "StockCountSheets", "StockCountLines",
        "StockReservations", "Waves", "WaveLines",
        "PickTasks", "PickTaskScanLogs",
        "WarehousePeriodLocks",
        "AppUsers", "AppRoles",
        "AuditLogs", "AiOcrLogs", "AiOcrAdjustments",
        "LoginAuditLogs", "MfaLoginChallenges",
        "Permissions", "RolePermissions",
        "BillOfMaterials"
    };

    /// <summary>
    /// Validates a table name against the whitelist and escapes brackets for safe SQL use.
    /// Returns null if the table name is not in the whitelist.
    /// </summary>
    public static string? SafeTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName)) return null;
        if (!ValidTableNames.Contains(tableName)) return null;
        return "[" + tableName.Replace("]", "]]") + "]";
    }

    /// <summary>
    /// Warehouse capacity constants — eliminates magic numbers.
    /// </summary>
    public static class WarehouseCapacity
    {
        /// <summary>Max weight in kg for standard storage locations.</summary>
        public const decimal MaxStorageKg = 2000m;

        /// <summary>Max volume in liters for chemical/liquid locations.</summary>
        public const decimal MaxChemicalLiters = 50000m;

        /// <summary>Unit label for weight-based capacity.</summary>
        public const string WeightUnit = "kg";

        /// <summary>Unit label for volume-based capacity.</summary>
        public const string VolumeUnit = "Lít";
    }

    /// <summary>
    /// File upload validation constants and helpers.
    /// </summary>
    public static class FileUpload
    {
        public static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        public static readonly HashSet<string> AllowedImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };

        public const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB
        public const long MaxOcrImageSizeBytes = 4 * 1024 * 1024; // 4MB

        /// <summary>
        /// Validates file extension AND content type for image uploads.
        /// </summary>
        public static bool IsValidImage(string? fileName, string? contentType, long length)
        {
            if (string.IsNullOrWhiteSpace(fileName) || length <= 0) return false;

            var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? "";
            if (!AllowedImageExtensions.Contains(ext)) return false;

            if (!string.IsNullOrWhiteSpace(contentType) && !AllowedImageMimeTypes.Contains(contentType))
                return false;

            return true;
        }

        /// <summary>
        /// P2-3: defense-in-depth — đọc magic bytes header để xác định file thật sự là ảnh.
        /// Chỉ check khi caller cần thêm tầng kiểm tra ngoài extension+mime.
        /// Stream sẽ được seek về 0 sau khi đọc.
        /// </summary>
        public static bool IsValidImageContent(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek) return false;
            var origin = stream.Position;
            try
            {
                Span<byte> header = stackalloc byte[8];
                stream.Position = 0;
                var read = stream.Read(header);
                if (read < 4) return false;
                // JPEG: FF D8 FF
                if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
                // PNG: 89 50 4E 47 0D 0A 1A 0A
                if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                    && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A) return true;
                // GIF: "GIF8"
                if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) return true;
                // WEBP: "RIFF" + ... + "WEBP" (chỉ check RIFF header tối thiểu)
                if (header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46) return true;
                return false;
            }
            finally
            {
                stream.Position = origin;
            }
        }
    }
}
