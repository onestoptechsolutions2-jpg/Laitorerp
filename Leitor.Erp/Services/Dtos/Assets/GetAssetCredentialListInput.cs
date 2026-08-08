using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Assets;

public class GetAssetCredentialListInput : PagedAndSortedResultRequestDto
{
    public Guid? ConfigurationItemId { get; set; }
}
