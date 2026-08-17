using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Hr;

public class NssfTierDto : FullAuditedEntityDto<Guid>
{
    public int TierNumber { get; set; }
    public decimal LowerBound { get; set; }
    public decimal UpperBound { get; set; }
    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public DateTime EffectiveFrom { get; set; }
}
