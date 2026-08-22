using System;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sms;

namespace Leitor.Erp.Services.Dtos.Sms;

public class BulkSmsQueueInput
{
    public string Content { get; set; } = string.Empty;
    public BulkSmsRecipientType Source { get; set; }

    // Only used when Source == Lead - same filter shape as Leads/Index.cshtml.cs.
    public LeadStatus? LeadStatus { get; set; }
    public Guid? AssignedToUserId { get; set; }

    // Only used when Source == Customer.
    public CustomerStatus? CustomerStatus { get; set; }

    // Only used when Source == Manual - one phone number per line/comma.
    public string? ManualPhoneNumbers { get; set; }
}
