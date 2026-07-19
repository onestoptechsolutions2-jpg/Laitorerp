using System;
using Leitor.Erp.Entities.Opportunities;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Opportunities;

public class OpportunityDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public OpportunityStatus Status { get; set; }
    public decimal? EstimatedValue { get; set; }
    public DateTime? ExpectedCloseDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? LostReason { get; set; }
    public string? Notes { get; set; }

    // Resolved by OpportunityAppService - not stored columns.
    public string? CustomerName { get; set; }
    public string? AssignedToUserName { get; set; }
}
