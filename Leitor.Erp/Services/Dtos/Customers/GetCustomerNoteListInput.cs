using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class GetCustomerNoteListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
}
