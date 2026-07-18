using System;
using Leitor.Erp.Entities.Customers;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class LeadTouchDto : FullAuditedEntityDto<Guid>
{
    public Guid LeadId { get; set; }
    public LeadChannel Channel { get; set; }
    public LeadDirection Direction { get; set; }
    public string? Notes { get; set; }
    public DateTime TouchedAt { get; set; }

    // Resolved by LeadTouchAppService from IdentityUser repository - not a stored column.
    public string? CreatorUserName { get; set; }
}
