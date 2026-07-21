using System;
using Leitor.Erp.Entities.Assets;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Assets;

public class ConfigurationItemRelationshipDto : EntityDto<Guid>
{
    public Guid SourceCiId { get; set; }
    public Guid TargetCiId { get; set; }
    public ConfigurationItemRelationshipType RelationshipType { get; set; }

    // Resolved by ConfigurationItemRelationshipAppService - not a stored column.
    public string? TargetCiName { get; set; }
}
