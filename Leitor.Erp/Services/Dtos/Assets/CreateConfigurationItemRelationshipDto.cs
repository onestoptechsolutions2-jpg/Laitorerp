using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Assets;

namespace Leitor.Erp.Services.Dtos.Assets;

public class CreateConfigurationItemRelationshipDto
{
    [Required]
    public Guid SourceCiId { get; set; }

    [Required]
    public Guid TargetCiId { get; set; }

    public ConfigurationItemRelationshipType RelationshipType { get; set; } = ConfigurationItemRelationshipType.DependsOn;
}
