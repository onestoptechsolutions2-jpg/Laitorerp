using System;
using Leitor.Erp.Entities.Partners;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Partners;

public class PartnerDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public PartnerCategory Category { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public CommissionBasis CommissionBasis { get; set; }
    public decimal CommissionRate { get; set; }
    public CommissionTrigger CommissionTrigger { get; set; }
}
