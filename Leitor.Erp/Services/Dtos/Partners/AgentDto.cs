using System;
using Leitor.Erp.Entities.Partners;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Partners;

public class AgentDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Territory { get; set; }
    public string? Skills { get; set; }
    public string? Notes { get; set; }
    public CommissionBasis CommissionBasis { get; set; }
    public decimal CommissionRate { get; set; }
    public CommissionTrigger CommissionTrigger { get; set; }
}
