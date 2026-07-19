using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class PriceListDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
}
