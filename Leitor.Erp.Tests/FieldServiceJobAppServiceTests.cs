using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.FieldService;
using Xunit;

namespace Leitor.Erp.Tests;

public class FieldServiceJobAppServiceTests : ErpTestBase
{
    [Fact]
    public async Task Transitioning_To_Completed_Sets_CompletedDate()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var jobAppService = GetRequiredService<FieldServiceJobAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Globex Corp" });

        var job = await jobAppService.CreateAsync(new CreateUpdateFieldServiceJobDto
        {
            CustomerId = customer.Id,
            Type = FieldServiceJobType.Installation,
            Status = FieldServiceJobStatus.Scheduled,
            ScheduledDate = DateTime.UtcNow
        });

        Assert.Null(job.CompletedDate);

        var updated = await jobAppService.UpdateAsync(job.Id, new CreateUpdateFieldServiceJobDto
        {
            CustomerId = customer.Id,
            Type = FieldServiceJobType.Installation,
            Status = FieldServiceJobStatus.Completed,
            ScheduledDate = job.ScheduledDate
        });

        Assert.NotNull(updated.CompletedDate);
    }

    [Fact]
    public async Task Reopening_A_Completed_Job_Clears_CompletedDate()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var jobAppService = GetRequiredService<FieldServiceJobAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Initech" });

        var job = await jobAppService.CreateAsync(new CreateUpdateFieldServiceJobDto
        {
            CustomerId = customer.Id,
            Status = FieldServiceJobStatus.Completed,
            ScheduledDate = DateTime.UtcNow
        });
        Assert.NotNull(job.CompletedDate);

        var reopened = await jobAppService.UpdateAsync(job.Id, new CreateUpdateFieldServiceJobDto
        {
            CustomerId = customer.Id,
            Status = FieldServiceJobStatus.Scheduled,
            ScheduledDate = job.ScheduledDate
        });

        Assert.Null(reopened.CompletedDate);
    }
}
