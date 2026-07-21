using System;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class TaxRateDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Percent { get; set; }
    public bool IsDefault { get; set; }
    public TaxType TaxType { get; set; }
}
