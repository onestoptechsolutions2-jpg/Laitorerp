using System;
using Leitor.Erp.Entities.Partners;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Partners;

public class CommissionDto : FullAuditedEntityDto<Guid>
{
    public Guid OpportunityId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? AgentId { get; set; }
    public CommissionBasis Basis { get; set; }
    public decimal Rate { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal Amount { get; set; }
    public CommissionTrigger Trigger { get; set; }
    public CommissionStatus Status { get; set; }
    public Guid? SourceInvoiceId { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }

    // Resolved by CommissionAppService - not stored columns.
    public string? OpportunityName { get; set; }
    public string? PartnerName { get; set; }
    public string? AgentName { get; set; }
}
