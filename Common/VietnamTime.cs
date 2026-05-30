namespace WMS.Common;

public static class VietnamTime
{
    public const string IanaTimeZoneId = "Asia/Ho_Chi_Minh";

    private static readonly TimeZoneInfo VnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : IanaTimeZoneId);

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTimeZone);

    public static DateTime Today => Now.Date;

    public static string FileStamp(string format = "yyyyMMddHHmmss")
        => Now.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
}
