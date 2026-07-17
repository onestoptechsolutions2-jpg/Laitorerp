using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Support;

public class GetTicketMessageListInput : PagedAndSortedResultRequestDto
{
    public Guid? TicketId { get; set; }
}
