using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class ProductBundleItemDto : FullAuditedEntityDto<Guid>
{
    public Guid BundleProductId { get; set; }
    public Guid ComponentProductId { get; set; }
    public decimal Quantity { get; set; }

    // Resolved by ProductBundleItemAppService - not a stored column.
    public string? ComponentProductName { get; set; }
}
