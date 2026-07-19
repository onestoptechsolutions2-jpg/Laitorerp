using System;
using Leitor.Erp.Entities.Support;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Support;

public class WarrantyClaimDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? TicketId { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WarrantyClaimStatus Status { get; set; }
    public DateTime FiledDate { get; set; }
    public DateTime? ResolvedDate { get; set; }

    // Resolved by WarrantyClaimAppService - not a stored column.
    public string? CustomerName { get; set; }
}
