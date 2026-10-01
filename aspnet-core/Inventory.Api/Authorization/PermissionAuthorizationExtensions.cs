using Inventory.Application.Authorization;

namespace Inventory.Api.Authorization;

public static class PermissionAuthorizationExtensions
{
    private const string PermissionClaimType = "permission";

    public static IServiceCollection AddPermissionAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in PermissionCodes.All)
            {
                options.AddPolicy(
                    permission,
                    policy => policy.RequireClaim(PermissionClaimType, permission));
            }
        });

        return services;
    }
}
