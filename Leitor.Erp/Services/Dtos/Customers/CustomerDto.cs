using System;
using Leitor.Erp.Entities.Customers;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CustomerDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public CustomerStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid? AccountOwnerUserId { get; set; }

    // Resolved by CustomerAppService from IIdentityUserRepository - not a stored column,
    // so Mapperly won't map it (Customer has no matching source member; that's fine, it's
    // filled in manually after the entity->dto mapping runs).
    public string? AccountOwnerUserName { get; set; }
}
