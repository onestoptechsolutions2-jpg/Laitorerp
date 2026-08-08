using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Procurement;

public class GetVendorContactListInput : PagedAndSortedResultRequestDto
{
    public Guid? VendorId { get; set; }
}