using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Hr;

public class PayeTaxBandDto : FullAuditedEntityDto<Guid>
{
    public decimal LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; }
}
