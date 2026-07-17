using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class VendorDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? Notes { get; set; }
}
