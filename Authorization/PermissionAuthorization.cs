using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace WMS.Authorization;

public static class PermissionClaimTypes
{
    public const string Permission = "perm";
}

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;
    public string Permission { get; }
}

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // Admin override: if role claim says Admin, allow all policies.
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var has = context.User.Claims.Any(c =>
            string.Equals(c.Type, PermissionClaimTypes.Permission, StringComparison.Ordinal)
            && string.Equals(c.Value, requirement.Permission, StringComparison.Ordinal));

        if (has)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

