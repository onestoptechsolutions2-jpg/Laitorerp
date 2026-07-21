using System;
using Leitor.Erp.Entities.Assets;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Assets;

public class GetConfigurationItemListInput : PagedAndSortedResultRequestDto
{
    public Guid? CustomerId { get; set; }
    public ConfigurationItemStatus? Status { get; set; }
    public string? Filter { get; set; }
}
