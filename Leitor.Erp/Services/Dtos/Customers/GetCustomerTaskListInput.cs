using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class GetCustomerTaskListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
}
