namespace WMS.Services;

public sealed class WorkflowResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public string? Warning { get; init; }
    public string? RedirectAction { get; init; }
    public object? RedirectRouteValues { get; init; }
    public bool NotFound { get; init; }
    public bool Forbidden { get; init; }

    public static WorkflowResult Success(string message, string? redirectAction = null, object? redirectRouteValues = null, string? warning = null)
        => new() { Succeeded = true, Message = message, Warning = warning, RedirectAction = redirectAction, RedirectRouteValues = redirectRouteValues };

    public static WorkflowResult Failure(string message, string? redirectAction = null, object? redirectRouteValues = null)
        => new() { Succeeded = false, Message = message, RedirectAction = redirectAction, RedirectRouteValues = redirectRouteValues };

    public static WorkflowResult NotFoundResult(string message)
        => new() { NotFound = true, Message = message };

    public static WorkflowResult ForbiddenResult()
        => new() { Forbidden = true };
}
