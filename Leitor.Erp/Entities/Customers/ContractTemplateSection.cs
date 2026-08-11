using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

// One clause block of a ContractTemplate, rendered in Order. Heading is null for the opening
// recitals/WHEREAS block (rendered as plain text, no bold heading); every numbered "SECTION N:"
// clause has one. BodyText carries [Placeholder] tokens substituted by ContractTemplateRenderer at
// PDF-generation time (see Documents/ContractPdfDocument.cs) - same bracket convention the source
// legal document already used, so seed text is close to a verbatim paste.
public class ContractTemplateSection : FullAuditedAggregateRoot<Guid>
{
    public Guid ContractTemplateId { get; set; }
    public int Order { get; set; }
    public string? Heading { get; set; }
    public string BodyText { get; set; } = string.Empty;

    protected ContractTemplateSection()
    {
    }

    public ContractTemplateSection(Guid id, Guid contractTemplateId, int order, string bodyText)
        : base(id)
    {
        ContractTemplateId = contractTemplateId;
        Order = order;
        BodyText = bodyText;
    }
}
