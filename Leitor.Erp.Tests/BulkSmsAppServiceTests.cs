using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sms;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Sms;
using Leitor.Erp.Services.Sms;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers BulkSmsAppService.QueueBatchAsync - the fast, network-free queuing step (see
// BulkSmsDispatchWorker for the actual throttled httpSMS send, not covered here since it needs a
// real/mocked HttpClient - see HttpSmsClientTests instead).
public class BulkSmsAppServiceTests : ErpTestBase
{
    private async Task EnableFeatureAsync()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.BulkSms, "true", "T", null);
    }

    [Fact]
    public async Task QueueBatchAsync_Manual_Dedups_And_Skips_Invalid_Numbers()
    {
        await EnableFeatureAsync();
        var bulkSmsAppService = GetRequiredService<BulkSmsAppService>();

        var result = await bulkSmsAppService.QueueBatchAsync(new BulkSmsQueueInput
        {
            Content = "Hello there",
            Source = BulkSmsRecipientType.Manual,
            ManualPhoneNumbers = "0712345678\n+254712345678\nnot-a-number\n0798765432"
        });

        // 0712345678 and +254712345678 normalize to the same E.164 number and dedup to one row;
        // "not-a-number" has no digits so it's skipped; 0798765432 is a distinct second recipient.
        Assert.Equal(2, result.QueuedCount);
        Assert.Equal(1, result.SkippedCount);

        var messageRepository = GetRequiredService<IRepository<BulkSmsMessage, Guid>>();
        var messages = await messageRepository.GetListAsync();
        Assert.Equal(2, messages.Count);
        Assert.All(messages, x => Assert.Equal(BulkSmsMessageStatus.Queued, x.Status));
        Assert.Contains(messages, x => x.ToPhoneNumber == "+254712345678");
        Assert.Contains(messages, x => x.ToPhoneNumber == "+254798765432");
    }

    [Fact]
    public async Task QueueBatchAsync_Leads_Excludes_DoNotContact()
    {
        await EnableFeatureAsync();

        var leadAppService = GetRequiredService<LeadAppService>();
        await leadAppService.CreateAsync(new CreateUpdateLeadDto { Name = "Contactable Lead", Phone = "0711111111" });
        await leadAppService.CreateAsync(new CreateUpdateLeadDto { Name = "Opted Out Lead", Phone = "0722222222", DoNotContact = true });

        var bulkSmsAppService = GetRequiredService<BulkSmsAppService>();
        var result = await bulkSmsAppService.QueueBatchAsync(new BulkSmsQueueInput
        {
            Content = "Hello leads",
            Source = BulkSmsRecipientType.Lead
        });

        Assert.Equal(1, result.QueuedCount);

        var messageRepository = GetRequiredService<IRepository<BulkSmsMessage, Guid>>();
        var messages = await messageRepository.GetListAsync();
        Assert.Single(messages);
        Assert.Equal("+254711111111", messages.Single().ToPhoneNumber);
    }
}
