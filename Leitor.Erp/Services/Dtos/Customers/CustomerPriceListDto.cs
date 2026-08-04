using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CustomerPriceListDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public Guid PriceListId { get; set; }
    public bool IsPrimary { get; set; }

    // Resolved by service - not a stored column
    public string? PriceListName { get; set; }
}
