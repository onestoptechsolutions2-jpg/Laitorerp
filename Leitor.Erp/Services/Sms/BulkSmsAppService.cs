using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sms;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Sms;

// Queuing (this class) is deliberately separate from dispatch (BulkSmsDispatchWorker): queuing is
// fast repository-only work safe to run inside one HTTP request even for a few hundred
// recipients, while actual sending has to be throttled to the gateway phone's messages-per-minute
// limit, which only a background worker can do without blocking the admin's request.
[RequiresFeature(ErpFeatures.BulkSms)]
public class BulkSmsAppService : ApplicationService
{
    private readonly IRepository<BulkSmsMessage, Guid> _messageRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IHttpSmsClient _httpSmsClient;

    public BulkSmsAppService(
        IRepository<BulkSmsMessage, Guid> messageRepository,
        IRepository<Lead, Guid> leadRepository,
        IRepository<Customer, Guid> customerRepository,
        IHttpSmsClient httpSmsClient)
    {
        _messageRepository = messageRepository;
        _leadRepository = leadRepository;
        _customerRepository = customerRepository;
        _httpSmsClient = httpSmsClient;
    }

    public async Task<BulkSmsQueueResultDto> QueueBatchAsync(BulkSmsQueueInput input)
    {
        await CheckSendPolicyAsync();

        var recipients = input.Source switch
        {
            BulkSmsRecipientType.Lead => await ResolveLeadRecipientsAsync(input),
            BulkSmsRecipientType.Customer => await ResolveCustomerRecipientsAsync(input),
            BulkSmsRecipientType.Manual => ResolveManualRecipients(input.ManualPhoneNumbers),
            _ => throw new ArgumentOutOfRangeException(nameof(input.Source))
        };

        var deduped = new Dictionary<string, (string? Name, Guid? EntityId)>();
        var skipped = 0;
        foreach (var recipient in recipients)
        {
            var e164 = PhoneLinks.ToE164(recipient.Phone);
            if (e164 == null)
            {
                skipped++;
                continue;
            }

            deduped.TryAdd(e164, (recipient.Name, recipient.EntityId));
        }

        var content = input.Content.Trim();
        var batchId = GuidGenerator.Create();
        var messages = deduped.Select(entry => new BulkSmsMessage(
                GuidGenerator.Create(),
                batchId,
                entry.Key,
                content,
                input.Source)
            {
                RecipientEntityId = entry.Value.EntityId,
                RecipientName = entry.Value.Name
            })
            .ToList();

        if (messages.Count > 0)
        {
            await _messageRepository.InsertManyAsync(messages);
        }

        return new BulkSmsQueueResultDto { QueuedCount = messages.Count, SkippedCount = skipped };
    }

    public async Task<HttpSmsSendResult> SendTestAsync(string phoneNumber, string content)
    {
        await CheckSendPolicyAsync();

        var e164 = PhoneLinks.ToE164(phoneNumber);
        if (e164 == null)
        {
            return new HttpSmsSendResult { Success = false, ErrorMessage = "That doesn't look like a valid phone number." };
        }

        return await _httpSmsClient.SendAsync(e164, content);
    }

    public async Task<List<BulkSmsBatchDto>> GetBatchesAsync()
    {
        await CheckSendPolicyAsync();

        var messages = await _messageRepository.GetListAsync();
        return messages
            .GroupBy(x => x.BatchId)
            .Select(group => new BulkSmsBatchDto
            {
                BatchId = group.Key,
                ContentPreview = Truncate(group.First().Content, 80),
                CreationTime = group.Min(x => x.CreationTime),
                TotalCount = group.Count(),
                QueuedCount = group.Count(x => x.Status == BulkSmsMessageStatus.Queued),
                SendingCount = group.Count(x => x.Status == BulkSmsMessageStatus.Sending),
                SentCount = group.Count(x => x.Status == BulkSmsMessageStatus.Sent),
                FailedCount = group.Count(x => x.Status == BulkSmsMessageStatus.Failed)
            })
            .OrderByDescending(x => x.CreationTime)
            .ToList();
    }

    public async Task<List<BulkSmsMessageDto>> GetBatchMessagesAsync(Guid batchId)
    {
        await CheckSendPolicyAsync();

        var messages = await _messageRepository.GetListAsync(x => x.BatchId == batchId);
        return messages
            .OrderBy(x => x.RecipientName)
            .Select(x => new BulkSmsMessageDto
            {
                Id = x.Id,
                ToPhoneNumber = x.ToPhoneNumber,
                RecipientName = x.RecipientName,
                RecipientType = x.RecipientType,
                Status = x.Status,
                ErrorMessage = x.ErrorMessage,
                SentAt = x.SentAt
            })
            .ToList();
    }

    private async Task<IEnumerable<(string? Phone, string? Name, Guid? EntityId)>> ResolveLeadRecipientsAsync(BulkSmsQueueInput input)
    {
        var leads = await _leadRepository.GetListAsync(x =>
            !x.DoNotContact &&
            (input.LeadStatus == null || x.Status == input.LeadStatus) &&
            (input.AssignedToUserId == null || x.AssignedToUserId == input.AssignedToUserId));

        return leads.Select(x => ((string?)x.Phone, (string?)x.Name, (Guid?)x.Id));
    }

    private async Task<IEnumerable<(string? Phone, string? Name, Guid? EntityId)>> ResolveCustomerRecipientsAsync(BulkSmsQueueInput input)
    {
        var customers = await _customerRepository.GetListAsync(x =>
            input.CustomerStatus == null || x.Status == input.CustomerStatus);

        return customers.Select(x => ((string?)x.PhoneNumber, (string?)x.Name, (Guid?)x.Id));
    }

    private static IEnumerable<(string? Phone, string? Name, Guid? EntityId)> ResolveManualRecipients(string? rawPhoneNumbers)
    {
        if (string.IsNullOrWhiteSpace(rawPhoneNumbers))
        {
            return Enumerable.Empty<(string?, string?, Guid?)>();
        }

        return rawPhoneNumbers
            .Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Select(phone => ((string?)phone, (string?)null, (Guid?)null));
    }

    private async Task CheckSendPolicyAsync()
    {
        if (!await AuthorizationService.IsGrantedAsync(ErpPermissions.BulkSms.Send))
        {
            throw new AbpAuthorizationException();
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "…";
    }
}
