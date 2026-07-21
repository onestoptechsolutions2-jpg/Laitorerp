using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Assets;

// The minimal CI-relationship graph ITIL4's own CMDB definition asks for - just enough to answer
// "what does this asset depend on / connect to" without building a full topology/discovery engine.
public class ConfigurationItemRelationship : FullAuditedAggregateRoot<Guid>
{
    public Guid SourceCiId { get; set; }
    public Guid TargetCiId { get; set; }
    public ConfigurationItemRelationshipType RelationshipType { get; set; } = ConfigurationItemRelationshipType.DependsOn;

    protected ConfigurationItemRelationship()
    {
    }

    public ConfigurationItemRelationship(Guid id, Guid sourceCiId, Guid targetCiId)
        : base(id)
    {
        SourceCiId = sourceCiId;
        TargetCiId = targetCiId;
    }
}
