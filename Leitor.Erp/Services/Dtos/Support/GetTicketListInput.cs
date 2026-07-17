using System;
using Leitor.Erp.Entities.Support;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Support;

public class GetTicketListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public string? Filter { get; set; }
}
