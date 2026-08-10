using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Support;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the per-customer SLA performance summary - the ELA-style number ITIL v5 formalizes,
// closing the "per-customer, not just org-wide" gap flagged in the ITIL v5 audit. Directly
// manipulates Ticket.ResolvedDate/SlaDueDate via the repository after creation to arrange precise
// met/breached scenarios, rather than waiting on real elapsed time - same technique
// GeneralLedgerReportAppServiceTests uses to arrange a same-day-timestamp entry.
public class SupportAnalyticsAppServiceTests : ErpTestBase
{
    [Fact]
    public async Task GetCustomerSlaPerformanceAsync_With_No_Tickets_Returns_Null_Compliance()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var analyticsAppService = GetRequiredService<SupportAnalyticsAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "No Tickets Ltd" });

        var result = await analyticsAppService.GetCustomerSlaPerformanceAsync(customer.Id);

        Assert.Equal(0, result.TotalCount);
        Assert.Null(result.CompliancePercent);
    }

    [Fact]
    public async Task GetCustomerSlaPerformanceAsync_Computes_Compliance_From_Met_And_Breached_Tickets()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var ticketAppService = GetRequiredService<TicketAppService>();
        var analyticsAppService = GetRequiredService<SupportAnalyticsAppService>();
        var ticketRepository = GetRequiredService<IRepository<Ticket, Guid>>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Acme Retainer Client" });

        var met = await ticketAppService.CreateAsync(new CreateUpdateTicketDto { CustomerId = customer.Id, Subject = "Met SLA" });
        var breached1 = await ticketAppService.CreateAsync(new CreateUpdateTicketDto { CustomerId = customer.Id, Subject = "Breached 1" });
        var breached2 = await ticketAppService.CreateAsync(new CreateUpdateTicketDto { CustomerId = customer.Id, Subject = "Breached 2" });

        // Arrange precise met/breached outcomes directly - SlaDueDate was already auto-computed on
        // create (see TicketAppService.ResolveSlaWindowAsync); resolving before it is "met",
        // after it is "breached", matching SupportAnalyticsAppService's own WasBreached logic.
        var metEntity = await ticketRepository.GetAsync(met.Id);
        metEntity.ResolvedDate = metEntity.SlaDueDate!.Value.AddHours(-1);
        await ticketRepository.UpdateAsync(metEntity, autoSave: true);

        var breached1Entity = await ticketRepository.GetAsync(breached1.Id);
        breached1Entity.ResolvedDate = breached1Entity.SlaDueDate!.Value.AddHours(1);
        await ticketRepository.UpdateAsync(breached1Entity, autoSave: true);

        var breached2Entity = await ticketRepository.GetAsync(breached2.Id);
        breached2Entity.ResolvedDate = breached2Entity.SlaDueDate!.Value.AddHours(1);
        await ticketRepository.UpdateAsync(breached2Entity, autoSave: true);

        var result = await analyticsAppService.GetCustomerSlaPerformanceAsync(customer.Id);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.MetCount);
        Assert.Equal(2, result.BreachedCount);
        Assert.Equal(33.3m, result.CompliancePercent);
    }

    [Fact]
    public async Task GetCustomerSlaPerformanceAsync_Only_Counts_This_Customers_Tickets()
    {
        await EnsureDatabaseCreatedAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var ticketAppService = GetRequiredService<TicketAppService>();
        var analyticsAppService = GetRequiredService<SupportAnalyticsAppService>();

        var customerA = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Customer A" });
        var customerB = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Customer B" });

        await ticketAppService.CreateAsync(new CreateUpdateTicketDto { CustomerId = customerA.Id, Subject = "A's ticket" });
        await ticketAppService.CreateAsync(new CreateUpdateTicketDto { CustomerId = customerB.Id, Subject = "B's ticket 1" });
        await ticketAppService.CreateAsync(new CreateUpdateTicketDto { CustomerId = customerB.Id, Subject = "B's ticket 2" });

        var resultA = await analyticsAppService.GetCustomerSlaPerformanceAsync(customerA.Id);
        var resultB = await analyticsAppService.GetCustomerSlaPerformanceAsync(customerB.Id);

        Assert.Equal(1, resultA.TotalCount);
        Assert.Equal(2, resultB.TotalCount);
    }
}
