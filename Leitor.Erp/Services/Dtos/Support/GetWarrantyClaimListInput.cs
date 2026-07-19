using System;
using Leitor.Erp.Entities.Support;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Support;

public class GetWarrantyClaimListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public WarrantyClaimStatus? Status { get; set; }
    public string? Filter { get; set; }
}
