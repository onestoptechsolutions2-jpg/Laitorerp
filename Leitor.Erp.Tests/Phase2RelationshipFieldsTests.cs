using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Dtos.Projects;
using Leitor.Erp.Services.Dtos.ServiceCatalog;
using Leitor.Erp.Services.Partners;
using Leitor.Erp.Services.Projects;
using Leitor.Erp.Services.ServiceCatalog;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-17 cross-module relationship fields (Phase 2): ServiceCatalogItem.PartnerId
// and ProjectTask.AgentId/PartnerId - loose Guid references, same DependencyGuard convention as
// every other cross-aggregate link in this app. Both Partner/Agent (PartnerCommission) and the
// entities under test here (ServiceCatalog/ProjectManagement) are separate optional modules, so
// tests enable all the feature toggles involved.
public class Phase2RelationshipFieldsTests : ErpTestBase
{
    private async Task<Guid> CreatePartnerAsync()
    {
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.PartnerCommission, "true", "T", null);

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Acme Networking Partner" });
        return partner.Id;
    }

    private async Task<Guid> CreateAgentAsync()
    {
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.PartnerCommission, "true", "T", null);

        var agentAppService = GetRequiredService<AgentAppService>();
        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Field Agent Jane" });
        return agent.Id;
    }

    [Fact]
    public async Task ServiceCatalogItem_PartnerId_Round_Trips_And_Resolves_PartnerName()
    {
        await EnsureDatabaseCreatedAsync();
        var partnerId = await CreatePartnerAsync();

        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ServiceCatalog, "true", "T", null);

        var serviceCatalogItemAppService = GetRequiredService<ServiceCatalogItemAppService>();
        var item = await serviceCatalogItemAppService.CreateAsync(new CreateUpdateServiceCatalogItemDto
        {
            Name = "Network Infrastructure Management",
            PartnerId = partnerId
        });

        var reloaded = await serviceCatalogItemAppService.GetAsync(item.Id);
        Assert.Equal(partnerId, reloaded.PartnerId);
        Assert.Equal("Acme Networking Partner", reloaded.PartnerName);
    }

    [Fact]
    public async Task ProjectTask_AgentId_And_PartnerId_Round_Trip_And_Resolve_Names()
    {
        await EnsureDatabaseCreatedAsync();
        var agentId = await CreateAgentAsync();
        var partnerId = await CreatePartnerAsync();

        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ProjectManagement, "true", "T", null);

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Wayne Enterprises" });

        var projectAppService = GetRequiredService<ProjectAppService>();
        var project = await projectAppService.CreateAsync(new CreateUpdateProjectDto { CustomerId = customer.Id, Title = "CCTV rollout" });

        var projectTaskAppService = GetRequiredService<ProjectTaskAppService>();
        var task = await projectTaskAppService.CreateAsync(new CreateUpdateProjectTaskDto
        {
            ProjectId = project.Id,
            Title = "Site survey",
            AgentId = agentId,
            PartnerId = partnerId
        });

        var list = await projectTaskAppService.GetListAsync(new GetProjectTaskListInput { ProjectId = project.Id });
        var reloaded = Assert.Single(list.Items);
        Assert.Equal(agentId, reloaded.AgentId);
        Assert.Equal("Field Agent Jane", reloaded.AgentName);
        Assert.Equal(partnerId, reloaded.PartnerId);
        Assert.Equal("Acme Networking Partner", reloaded.PartnerName);
    }

    [Fact]
    public async Task Customer_Detail_ServiceRequests_Filters_By_CustomerId()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ServiceRequestManagement, "true", "T", null);

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customerA = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Customer A" });
        var customerB = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Customer B" });

        var serviceRequestAppService = GetRequiredService<Services.ServiceRequests.ServiceRequestAppService>();
        await serviceRequestAppService.CreateAsync(new Services.Dtos.ServiceRequests.CreateUpdateServiceRequestDto
        {
            CustomerId = customerA.Id,
            Description = "Need a new access badge",
            RequestedDate = DateTime.UtcNow
        });
        await serviceRequestAppService.CreateAsync(new Services.Dtos.ServiceRequests.CreateUpdateServiceRequestDto
        {
            CustomerId = customerB.Id,
            Description = "Unrelated request",
            RequestedDate = DateTime.UtcNow
        });

        var result = await serviceRequestAppService.GetListAsync(new Services.Dtos.ServiceRequests.GetServiceRequestListInput { CustomerId = customerA.Id });
        var item = Assert.Single(result.Items);
        Assert.Equal("Need a new access badge", item.Description);
        Assert.Equal("Customer A", item.CustomerName);
    }
}
