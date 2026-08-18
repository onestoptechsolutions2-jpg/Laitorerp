using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.ServiceRequests;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.ServiceRequests;
using Leitor.Erp.Services.ServiceRequests;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-18 consolidation audit's "give ServiceRequest a destination" fix -
// previously a ServiceRequest had no path to become billable work; ConvertToJobAsync gives it one
// (mirrors ProposalAppService.ConvertToQuoteAsync's "dedicated transition entry point" shape).
public class ServiceRequestAppServiceTests : ErpTestBase
{
    private async Task<Guid> CreateCustomerAsync()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.ServiceRequestManagement, "true", "T", null);

        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Convert Test Co" });
        return customer.Id;
    }

    [Fact]
    public async Task ConvertToJobAsync_Creates_A_FieldServiceJob_And_Links_It_Back()
    {
        var customerId = await CreateCustomerAsync();
        var serviceRequestAppService = GetRequiredService<ServiceRequestAppService>();

        var request = await serviceRequestAppService.CreateAsync(new CreateUpdateServiceRequestDto
        {
            CustomerId = customerId,
            Description = "Install a new access point in the boardroom",
            RequestedDate = DateTime.UtcNow
        });

        var jobId = await serviceRequestAppService.ConvertToJobAsync(request.Id);

        var jobRepository = GetRequiredService<IRepository<FieldServiceJob, Guid>>();
        var job = await jobRepository.GetAsync(jobId);
        Assert.Equal(customerId, job.CustomerId);
        Assert.Equal("Install a new access point in the boardroom", job.Description);

        var reloaded = await serviceRequestAppService.GetAsync(request.Id);
        Assert.Equal(jobId, reloaded.JobId);
        Assert.Equal(ServiceRequestStatus.InProgress, reloaded.Status);
    }

    [Fact]
    public async Task ConvertToJobAsync_Twice_Is_Blocked()
    {
        var customerId = await CreateCustomerAsync();
        var serviceRequestAppService = GetRequiredService<ServiceRequestAppService>();

        var request = await serviceRequestAppService.CreateAsync(new CreateUpdateServiceRequestDto
        {
            CustomerId = customerId,
            Description = "Replace a faulty switch",
            RequestedDate = DateTime.UtcNow
        });

        await serviceRequestAppService.ConvertToJobAsync(request.Id);

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => serviceRequestAppService.ConvertToJobAsync(request.Id));
        Assert.Equal("This service request has already been converted to a job.", ex.Message);
    }

    [Fact]
    public async Task ConvertToJobAsync_Blocked_Once_The_Request_Is_Closed()
    {
        var customerId = await CreateCustomerAsync();
        var serviceRequestAppService = GetRequiredService<ServiceRequestAppService>();

        var request = await serviceRequestAppService.CreateAsync(new CreateUpdateServiceRequestDto
        {
            CustomerId = customerId,
            Description = "One-off request that got rejected",
            RequestedDate = DateTime.UtcNow
        });

        await serviceRequestAppService.UpdateAsync(request.Id, new CreateUpdateServiceRequestDto
        {
            CustomerId = customerId,
            Description = request.Description,
            Status = ServiceRequestStatus.Rejected,
            RequestedDate = request.RequestedDate
        });

        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => serviceRequestAppService.ConvertToJobAsync(request.Id));
        Assert.Equal("This service request is already closed and can't be converted to a job.", ex.Message);
    }
}
