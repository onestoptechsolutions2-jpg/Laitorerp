using System;
using Leitor.Erp.Entities.Customers;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class LeadDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public LeadSource Source { get; set; }
    public LeadStatus Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public Guid? ConvertedCustomerId { get; set; }

    // Resolved by LeadAppService from IdentityUser repository - not a stored column.
    public string? AssignedToUserName { get; set; }
}
