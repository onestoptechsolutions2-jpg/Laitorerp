using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class GetLeadTouchListInput : PagedAndSortedResultRequestDto
{
    public Guid? LeadId { get; set; }
}
