using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Cybersecurity;
using Leitor.Erp.Services.Dtos.Assets;
using Leitor.Erp.Services.Dtos.Cybersecurity;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Projects;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Projects;
using Leitor.Erp.Services.Support;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// DeletionRequestAppService.DispatchDeleteAsync's switch only ever covered the original 7
// entities from the 2026-07-17/18 roles/permissions matrix initiative - Problem/Project/
// ConfigurationItem/SecurityAssessment all correctly file a DeletionRequest via their own
// DeleteAsync (DeletionGate.EnsureImmediateDeleteAllowedAsync), but approving that request threw
// "Unknown entity type" since none of the 4 were in the switch - flagged in
// [[fix_lead_deletion_cascade_2026-08-11]], fixed 2026-08-17. These tests seed a DeletionRequest
// directly via the repository (same technique as EscalationItemTests - AlwaysAllowAuthorizationService
// means the FILING branch itself isn't exercisable here) to prove ApproveAsync now dispatches to
// all 4 without throwing, and that the underlying entity is actually deleted.
public class DeletionRequestDispatchTests : ErpTestBase
{
    private async Task<Guid> SeedDeletionRequestAsync(string entityType, Guid entityId)
    {
        var repository = GetRequiredService<IRepository<DeletionRequest, Guid>>();
        var request = new DeletionRequest(Guid.NewGuid(), entityType, entityId, null, DateTime.UtcNow);
        await repository.InsertAsync(request, autoSave: true);
        return request.Id;
    }

    [Fact]
    public async Task Approving_A_Problem_Deletion_Request_Deletes_The_Problem()
    {
        await EnsureDatabaseCreatedAsync();
        var problemAppService = GetRequiredService<ProblemAppService>();
        var problem = await problemAppService.CreateAsync(new CreateUpdateProblemDto { Title = "Recurring VPN drops" });

        var requestId = await SeedDeletionRequestAsync("Problem", problem.Id);

        var deletionRequestAppService = GetRequiredService<DeletionRequestAppService>();
        await deletionRequestAppService.ApproveAsync(requestId); // must not throw

        var repository = GetRequiredService<IRepository<Entities.Support.Problem, Guid>>();
        Assert.Null(await repository.FindAsync(problem.Id));
    }

    [Fact]
    public async Task Approving_A_Project_Deletion_Request_Deletes_The_Project()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ProjectManagement, "true", "T", null);

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Stark Industries" });

        var projectAppService = GetRequiredService<ProjectAppService>();
        var project = await projectAppService.CreateAsync(new CreateUpdateProjectDto { CustomerId = customer.Id, Title = "Office network build-out" });

        var requestId = await SeedDeletionRequestAsync("Project", project.Id);

        var deletionRequestAppService = GetRequiredService<DeletionRequestAppService>();
        await deletionRequestAppService.ApproveAsync(requestId); // must not throw

        var repository = GetRequiredService<IRepository<Entities.Projects.Project, Guid>>();
        Assert.Null(await repository.FindAsync(project.Id));
    }

    [Fact]
    public async Task Approving_A_ConfigurationItem_Deletion_Request_Deletes_The_ConfigurationItem()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.AssetManagement, "true", "T", null);

        var ciAppService = GetRequiredService<ConfigurationItemAppService>();
        var ci = await ciAppService.CreateAsync(new CreateUpdateConfigurationItemDto { Name = "Core Switch" });

        var requestId = await SeedDeletionRequestAsync("ConfigurationItem", ci.Id);

        var deletionRequestAppService = GetRequiredService<DeletionRequestAppService>();
        await deletionRequestAppService.ApproveAsync(requestId); // must not throw

        var repository = GetRequiredService<IRepository<Entities.Assets.ConfigurationItem, Guid>>();
        Assert.Null(await repository.FindAsync(ci.Id));
    }

    [Fact]
    public async Task Approving_A_SecurityAssessment_Deletion_Request_Deletes_The_SecurityAssessment()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.Cybersecurity, "true", "T", null);

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Wayne Enterprises" });

        var assessmentAppService = GetRequiredService<SecurityAssessmentAppService>();
        var assessment = await assessmentAppService.CreateAsync(new CreateUpdateSecurityAssessmentDto
        {
            CustomerId = customer.Id,
            Title = "Annual vulnerability scan",
            ScheduledDate = DateTime.UtcNow.AddDays(7)
        });

        var requestId = await SeedDeletionRequestAsync("SecurityAssessment", assessment.Id);

        var deletionRequestAppService = GetRequiredService<DeletionRequestAppService>();
        await deletionRequestAppService.ApproveAsync(requestId); // must not throw

        var repository = GetRequiredService<IRepository<Entities.Cybersecurity.SecurityAssessment, Guid>>();
        Assert.Null(await repository.FindAsync(assessment.Id));
    }

    [Fact]
    public async Task Approving_A_Deletion_Request_For_An_Unmapped_Entity_Type_Still_Throws()
    {
        await EnsureDatabaseCreatedAsync();
        var requestId = await SeedDeletionRequestAsync("SomethingThatDoesNotExist", Guid.NewGuid());

        var deletionRequestAppService = GetRequiredService<DeletionRequestAppService>();
        await Assert.ThrowsAsync<UserFriendlyException>(() => deletionRequestAppService.ApproveAsync(requestId));
    }
}
