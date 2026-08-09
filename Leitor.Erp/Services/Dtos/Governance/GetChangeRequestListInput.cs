using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Governance;

public class GetChangeRequestListInput : PagedAndSortedResultRequestDto
{
    public Guid? ConfigurationItemId { get; set; }
}
