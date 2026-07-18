using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Sales;

public class ProductVendorDto : FullAuditedEntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public Guid VendorId { get; set; }
    public string? VendorSku { get; set; }
    public decimal Cost { get; set; }
    public int? LeadTimeDays { get; set; }
    public bool IsPreferred { get; set; }
    public string? Notes { get; set; }

    // Resolved by ProductVendorAppService - not a stored column.
    public string? VendorName { get; set; }
}
