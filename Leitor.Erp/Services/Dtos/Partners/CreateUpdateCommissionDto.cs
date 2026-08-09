using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Partners;

namespace Leitor.Erp.Services.Dtos.Partners;

// Deliberately has no Status property - a Commission's status only ever moves via
// CommissionAppService's own state-transition logic (creation, CommissionAutoPayableService, or
// MarkPaidAsync), never through a generic field edit. Editing Basis/Rate/BaseAmount recomputes
// Amount but never touches Status.
public class CreateUpdateCommissionDto
{
    [Required]
    public Guid OpportunityId { get; set; }

    public Guid? PartnerId { get; set; }
    public Guid? AgentId { get; set; }

    public CommissionBasis Basis { get; set; } = CommissionBasis.Percentage;
    public decimal Rate { get; set; }
    public decimal BaseAmount { get; set; }
    public CommissionTrigger Trigger { get; set; } = CommissionTrigger.OnClientPayment;
    public Guid? SourceInvoiceId { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
