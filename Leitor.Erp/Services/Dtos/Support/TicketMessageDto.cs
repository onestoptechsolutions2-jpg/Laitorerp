using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Support;

public class TicketMessageDto : FullAuditedEntityDto<Guid>
{
    public Guid TicketId { get; set; }
    public bool IsCustomerMessage { get; set; }
    public string Text { get; set; } = string.Empty;

    // Resolved by TicketMessageAppService from IdentityUser repository - not a stored column.
    public string? CreatorUserName { get; set; }
}
