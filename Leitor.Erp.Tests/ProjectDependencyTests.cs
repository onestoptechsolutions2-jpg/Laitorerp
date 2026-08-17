using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Projects;
using Leitor.Erp.Services.Projects;
using Volo.Abp;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers Project.DependsOnProjectId (2026-08-17) - one field for both the network-infra-before-
// CCTV blocking scenario and completed-project-spawns-a-follow-up lineage, per the user's own
// example. ProjectDependencyGuard mirrors FiscalPeriodGuard's shape (throw if the transition
// isn't allowed yet), checked when a Project's Status moves into Active.
public class ProjectDependencyTests : ErpTestBase
{
    private async Task<Guid> CreateCustomerAsync()
    {
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Wayne Enterprises" });
        return customer.Id;
    }

    [Fact]
    public async Task Starting_A_Project_Whose_Dependency_Is_Not_Completed_Throws()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ProjectManagement, "true", "T", null);

        var customerId = await CreateCustomerAsync();
        var projectAppService = GetRequiredService<ProjectAppService>();

        var networkInfra = await projectAppService.CreateAsync(new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = "Network Infrastructure Build-out",
            StartDate = DateTime.UtcNow,
            Status = ProjectStatus.Planned
        });

        var cctv = await projectAppService.CreateAsync(new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = "CCTV Installation",
            StartDate = DateTime.UtcNow,
            Status = ProjectStatus.Planned,
            DependsOnProjectId = networkInfra.Id
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => projectAppService.UpdateAsync(cctv.Id, new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = cctv.Title,
            StartDate = cctv.StartDate,
            Status = ProjectStatus.Active,
            DependsOnProjectId = networkInfra.Id
        }));
    }

    [Fact]
    public async Task Starting_A_Project_Whose_Dependency_Is_Completed_Succeeds()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ProjectManagement, "true", "T", null);

        var customerId = await CreateCustomerAsync();
        var projectAppService = GetRequiredService<ProjectAppService>();

        var networkInfra = await projectAppService.CreateAsync(new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = "Network Infrastructure Build-out",
            StartDate = DateTime.UtcNow,
            Status = ProjectStatus.Planned
        });
        await projectAppService.UpdateAsync(networkInfra.Id, new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = networkInfra.Title,
            StartDate = networkInfra.StartDate,
            Status = ProjectStatus.Completed
        });

        var cctv = await projectAppService.CreateAsync(new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = "CCTV Installation",
            StartDate = DateTime.UtcNow,
            Status = ProjectStatus.Planned,
            DependsOnProjectId = networkInfra.Id
        });

        var updated = await projectAppService.UpdateAsync(cctv.Id, new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = cctv.Title,
            StartDate = cctv.StartDate,
            Status = ProjectStatus.Active,
            DependsOnProjectId = networkInfra.Id
        });

        Assert.Equal(ProjectStatus.Active, updated.Status);
    }

    [Fact]
    public async Task GetAsync_Resolves_DependsOnProjectTitle()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ProjectManagement, "true", "T", null);

        var customerId = await CreateCustomerAsync();
        var projectAppService = GetRequiredService<ProjectAppService>();

        var original = await projectAppService.CreateAsync(new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = "Office Network Rollout",
            StartDate = DateTime.UtcNow
        });

        var followUp = await projectAppService.CreateAsync(new CreateUpdateProjectDto
        {
            CustomerId = customerId,
            Title = "Follow-up: Office Network Rollout",
            StartDate = DateTime.UtcNow,
            DependsOnProjectId = original.Id
        });

        var reloaded = await projectAppService.GetAsync(followUp.Id);
        Assert.Equal(original.Id, reloaded.DependsOnProjectId);
        Assert.Equal("Office Network Rollout", reloaded.DependsOnProjectTitle);
    }
}
