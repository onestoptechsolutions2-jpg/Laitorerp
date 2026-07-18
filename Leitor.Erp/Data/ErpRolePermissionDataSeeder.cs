using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.PermissionManagement;
using IdentityRole = Volo.Abp.Identity.IdentityRole;

namespace Leitor.Erp.Data;

// Seeds the 6 non-Admin roles from the approved roles/permissions matrix
// (https://claude.ai/code/artifact/5a4c038e-0018-47e2-9875-c79783878e25) and grants each one its
// module permissions, so RBAC ships as code instead of a manual /Identity/Roles setup step.
//
// Runs automatically: IDataSeedContributor implementations are picked up by ABP's IDataSeeder and
// invoked from ErpDbMigrationService.SeedDataAsync, which the `--migrate-database` argument runs -
// entrypoint.sh already does this on every container start. Re-running is safe: roles are only
// created if missing, and IPermissionManager.SetAsync is idempotent.
//
// Admin isn't listed here - ABP's own IdentityDataSeedContributor already creates it and grants it
// every defined permission automatically (it re-grants on every seed run, so it always has full
// access as new permissions are added), so it needs no entry in this table.
public class ErpRolePermissionDataSeeder : IDataSeedContributor, ITransientDependency
{
    // ABP's role-permission provider name (Volo.Abp.PermissionManagement.Identity.
    // RolePermissionManagementProvider.Name = "R"). Not exposed as a public constant from the
    // packages this project references, so it's a literal here rather than an import.
    private const string RoleProviderName = "R";

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IPermissionManager _permissionManager;
    private readonly IGuidGenerator _guidGenerator;

    public ErpRolePermissionDataSeeder(
        RoleManager<IdentityRole> roleManager,
        IPermissionManager permissionManager,
        IGuidGenerator guidGenerator)
    {
        _roleManager = roleManager;
        _permissionManager = permissionManager;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        foreach (var (roleName, permissions) in RoleDefinitions())
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new IdentityRole(_guidGenerator.Create(), roleName, context.TenantId)
                {
                    IsPublic = true
                };
                (await _roleManager.CreateAsync(role)).CheckErrors();
            }

            foreach (var permission in permissions)
            {
                await _permissionManager.SetAsync(permission, RoleProviderName, role.Name, true);
            }
        }
    }

    // Mirrors the approved matrix's grant levels: "Manage" => Default+Create+Edit+Delete on that
    // module, "View" => Default only, "—" => nothing. A "Manage" role still can't delete
    // anything immediately - holding the module's own Delete permission is only what lets
    // DeletionGate be reached at all (see Services/Governance/DeletionGate.cs); only Admin and Ops
    // Manager also hold DeletionApprovals.Decide, so every other role's delete always stops at
    // "request filed" regardless of holding the module Delete permission.
    //
    // AMC/call contracts (CustomerContractAppService) reuse the Customers permission group rather
    // than a permission of their own, so the matrix's separate "AMC / call contracts" row collapses
    // into whatever each role already holds for Customers below.
    private static IEnumerable<(string RoleName, string[] Permissions)> RoleDefinitions()
    {
        yield return ("Ops Manager", new[]
        {
            ErpPermissions.Leads.Default,
            ErpPermissions.Customers.Default,
            ErpPermissions.Catalog.Default,
            ErpPermissions.Vendors.Default,
            ErpPermissions.Sales.Default,
            ErpPermissions.Procurement.Default,
            ErpPermissions.FieldService.Default,
            ErpPermissions.Support.Default,
            ErpPermissions.AuditLogs.Default,
            ErpPermissions.DeletionApprovals.Default,
            ErpPermissions.DeletionApprovals.Decide
        });

        yield return ("Sales Agent", new[]
        {
            ErpPermissions.Leads.Default, ErpPermissions.Leads.Create, ErpPermissions.Leads.Edit, ErpPermissions.Leads.Delete,
            ErpPermissions.Customers.Default, ErpPermissions.Customers.Create, ErpPermissions.Customers.Edit, ErpPermissions.Customers.Delete,
            ErpPermissions.Catalog.Default,
            ErpPermissions.Vendors.Default,
            ErpPermissions.Sales.Default, ErpPermissions.Sales.Create, ErpPermissions.Sales.Edit, ErpPermissions.Sales.Delete,
            ErpPermissions.FieldService.Default,
            ErpPermissions.Support.Default,
            ErpPermissions.DeletionApprovals.Default
        });

        yield return ("Procurement", new[]
        {
            ErpPermissions.Customers.Default,
            ErpPermissions.Catalog.Default, ErpPermissions.Catalog.Create, ErpPermissions.Catalog.Edit, ErpPermissions.Catalog.Delete,
            ErpPermissions.Vendors.Default, ErpPermissions.Vendors.Create, ErpPermissions.Vendors.Edit, ErpPermissions.Vendors.Delete,
            ErpPermissions.Sales.Default,
            ErpPermissions.Procurement.Default, ErpPermissions.Procurement.Create, ErpPermissions.Procurement.Edit, ErpPermissions.Procurement.Delete,
            ErpPermissions.DeletionApprovals.Default
        });

        yield return ("Dispatcher", new[]
        {
            ErpPermissions.Customers.Default,
            ErpPermissions.Sales.Default,
            ErpPermissions.Procurement.Default,
            ErpPermissions.FieldService.Default, ErpPermissions.FieldService.Create, ErpPermissions.FieldService.Edit, ErpPermissions.FieldService.Delete,
            ErpPermissions.Support.Default,
            ErpPermissions.DeletionApprovals.Default
        });

        yield return ("Support", new[]
        {
            ErpPermissions.Customers.Default,
            ErpPermissions.Sales.Default,
            ErpPermissions.FieldService.Default,
            ErpPermissions.Support.Default, ErpPermissions.Support.Create, ErpPermissions.Support.Edit, ErpPermissions.Support.Delete,
            ErpPermissions.DeletionApprovals.Default
        });

        yield return ("Finance", new[]
        {
            ErpPermissions.Customers.Default,
            ErpPermissions.Sales.Default, ErpPermissions.Sales.Create, ErpPermissions.Sales.Edit, ErpPermissions.Sales.Delete,
            ErpPermissions.Procurement.Default,
            ErpPermissions.DeletionApprovals.Default
        });

        // Portal-only identities: no Erp.* permission granted. PortalUserId linkage is the sole
        // authorization for Client/Vendor Portal pages (see Pages/Portal/*), so these two roles
        // exist only to tag/organize portal accounts at /Identity/Users - not to grant access.
        yield return ("Client Portal", Array.Empty<string>());
        yield return ("Vendor Portal", Array.Empty<string>());
    }
}
