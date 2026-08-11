using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

// A reusable legal-agreement shape (e.g. "Managed Services Agreement") - the header. Its
// ContractTemplateSections carry the actual clause text with [Placeholder] tokens; the header
// itself only carries the metadata needed to pick and default a new CustomerContract from it (see
// ContractTemplateAppService, ContractPdfDocument).
public class ContractTemplate : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Prefills CreateUpdateCustomerContractDto's term when a CustomerContract picks this template
    // and has no EndDate yet - purely a UI convenience, never itself rendered onto the PDF ([TermMonths]
    // is computed from the contract's own StartDate/EndDate when set).
    public int? DefaultTermMonths { get; set; }

    protected ContractTemplate()
    {
    }

    public ContractTemplate(Guid id, string name)
        : base(id)
    {
        Name = name;
    }
}
