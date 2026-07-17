using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Authorization;

namespace Leitor.Erp.Tests;

// These tests exercise app service business logic (cascading deletes, computed payment status,
// status-transition side effects, etc.), not the permission system - with a real authorization
// service every CrudAppService call would be denied since the empty test database has no
// role/permission grants seeded. ABP's CheckPolicyAsync path requires IAbpAuthorizationService
// specifically (a plain IAuthorizationService isn't enough - it casts and throws), so this
// implements the ABP-specific interface, not just the ASP.NET Core one. Swapped in by
// ErpTestBase.ConfigureServices in place of the real implementation.
public class AlwaysAllowAuthorizationService : IAbpAuthorizationService
{
    public IServiceProvider ServiceProvider { get; }

    public ClaimsPrincipal CurrentPrincipal => new ClaimsPrincipal(new ClaimsIdentity());

    public AlwaysAllowAuthorizationService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        return Task.FromResult(AuthorizationResult.Success());
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        return Task.FromResult(AuthorizationResult.Success());
    }
}
