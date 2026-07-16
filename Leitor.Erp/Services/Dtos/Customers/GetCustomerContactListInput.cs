using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class GetCustomerContactListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
}
