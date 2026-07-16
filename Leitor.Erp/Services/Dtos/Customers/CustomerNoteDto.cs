using System;
using Leitor.Erp.Entities.Customers;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CustomerNoteDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public CustomerNoteType Type { get; set; }
    public string Text { get; set; } = string.Empty;

    // Resolved by CustomerNoteAppService from IIdentityUserRepository using CreatorId - not a
    // stored column, Mapperly won't map it.
    public string? CreatorUserName { get; set; }
}
