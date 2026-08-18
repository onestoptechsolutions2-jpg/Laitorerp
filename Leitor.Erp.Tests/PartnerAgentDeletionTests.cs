using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Calendar;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the UX/error-handling audit's DependencyGuard-gap pass: Partner/Agent are referenced by
// several other entities via nullable, loose Guid FKs (ProjectTask.PartnerId/AgentId,
// ServiceCatalogItem.PartnerId, CalendarEvent.AgentId) - PartnerAppService/AgentAppService already
// blocked deletion on Commission history and nulled Opportunity/Lead references, but left these
// four dangling, pointing at a row that no longer exists. Confirms the fix actually clears them
// instead of leaving a dangling reference.
public class PartnerAgentDeletionTests : ErpTestBase
{
    [Fact]
    public async Task PartnerAppService_DeleteAsync_Clears_ProjectTask_PartnerId()
    {
        await EnsureDatabaseCreatedAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var projectTaskRepository = GetRequiredService<IRepository<ProjectTask, Guid>>();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });
        var task = new ProjectTask(Guid.NewGuid(), Guid.NewGuid(), "Install POS terminals")
        {
            PartnerId = partner.Id
        };
        await projectTaskRepository.InsertAsync(task, autoSave: true);

        await partnerAppService.DeleteAsync(partner.Id);

        var reloaded = await projectTaskRepository.GetAsync(task.Id);
        Assert.Null(reloaded.PartnerId);
    }

    [Fact]
    public async Task PartnerAppService_DeleteAsync_Clears_ServiceCatalogItem_PartnerId()
    {
        await EnsureDatabaseCreatedAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var serviceCatalogItemRepository = GetRequiredService<IRepository<ServiceCatalogItem, Guid>>();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });
        var item = new ServiceCatalogItem(Guid.NewGuid(), "Managed Backup")
        {
            PartnerId = partner.Id
        };
        await serviceCatalogItemRepository.InsertAsync(item, autoSave: true);

        await partnerAppService.DeleteAsync(partner.Id);

        var reloaded = await serviceCatalogItemRepository.GetAsync(item.Id);
        Assert.Null(reloaded.PartnerId);
    }

    [Fact]
    public async Task AgentAppService_DeleteAsync_Clears_CalendarEvent_AgentId()
    {
        await EnsureDatabaseCreatedAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var calendarEventRepository = GetRequiredService<IRepository<CalendarEvent, Guid>>();

        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat" });
        var calendarEvent = new CalendarEvent(Guid.NewGuid(), "Site visit", DateTime.UtcNow)
        {
            AgentId = agent.Id
        };
        await calendarEventRepository.InsertAsync(calendarEvent, autoSave: true);

        await agentAppService.DeleteAsync(agent.Id);

        var reloaded = await calendarEventRepository.GetAsync(calendarEvent.Id);
        Assert.Null(reloaded.AgentId);
    }

    [Fact]
    public async Task AgentAppService_DeleteAsync_Clears_ProjectTask_AgentId()
    {
        await EnsureDatabaseCreatedAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var projectTaskRepository = GetRequiredService<IRepository<ProjectTask, Guid>>();

        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat" });
        var task = new ProjectTask(Guid.NewGuid(), Guid.NewGuid(), "Install POS terminals")
        {
            AgentId = agent.Id
        };
        await projectTaskRepository.InsertAsync(task, autoSave: true);

        await agentAppService.DeleteAsync(agent.Id);

        var reloaded = await projectTaskRepository.GetAsync(task.Id);
        Assert.Null(reloaded.AgentId);
    }
}
