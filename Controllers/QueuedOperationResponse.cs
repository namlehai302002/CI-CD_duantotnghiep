using Microsoft.AspNetCore.Mvc;

namespace WMS.Controllers;

internal static class QueuedOperationResponse
{
    private const string QueuedHeader = "X-WMS-Queued-Operation";
    private const string OperationIdHeader = "X-WMS-Offline-Operation-Id";

    public static bool IsQueued(Controller controller)
        => string.Equals(controller.Request.Headers[QueuedHeader].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase);

    public static string? OperationId(Controller controller)
        => controller.Request.Headers[OperationIdHeader].FirstOrDefault();

    public static JsonResult Json(Controller controller, bool success, string message, string? redirectUrl = null, int statusCode = 200, string? code = null)
    {
        var result = controller.Json(new
        {
            success,
            message,
            redirectUrl,
            operationId = OperationId(controller),
            code
        });
        result.StatusCode = statusCode;
        return result;
    }
}
