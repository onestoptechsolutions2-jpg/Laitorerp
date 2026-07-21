using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Assets;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Services.Assets;

// The CI-relationship graph is managed inline on ConfigurationItem Detail (add/remove), not its
// own top-level pages - same "child collection, no dedicated pages" shape as ProjectTask.
[RequiresFeature(ErpFeatures.AssetManagement)]
public class ConfigurationItemRelationshipAppService : ApplicationService
{
    private readonly IRepository<ConfigurationItemRelationship, Guid> _repository;
    private readonly IRepository<ConfigurationItem, Guid> _configurationItemRepository;

    public ConfigurationItemRelationshipAppService(
        IRepository<ConfigurationItemRelationship, Guid> repository,
        IRepository<ConfigurationItem, Guid> configurationItemRepository)
    {
        _repository = repository;
        _configurationItemRepository = configurationItemRepository;
    }

    public async Task<List<ConfigurationItemRelationshipDto>> GetListAsync(Guid sourceCiId)
    {
        await CheckPolicyAsync(ErpPermissions.Assets.Default);

        var relationships = await _repository.GetListAsync(x => x.SourceCiId == sourceCiId);
        var targetIds = relationships.Select(x => x.TargetCiId).Distinct().ToList();
        var namesById = targetIds.Count > 0
            ? (await _configurationItemRepository.GetListAsync(x => targetIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        return relationships.Select(x => new ConfigurationItemRelationshipDto
        {
            Id = x.Id,
            SourceCiId = x.SourceCiId,
            TargetCiId = x.TargetCiId,
            RelationshipType = x.RelationshipType,
            TargetCiName = namesById.GetValueOrDefault(x.TargetCiId)
        }).ToList();
    }

    public async Task<ConfigurationItemRelationshipDto> CreateAsync(CreateConfigurationItemRelationshipDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Assets.Edit);

        if (input.SourceCiId == input.TargetCiId)
        {
            throw new UserFriendlyException("A configuration item can't have a relationship with itself.");
        }

        var entity = new ConfigurationItemRelationship(GuidGenerator.Create(), input.SourceCiId, input.TargetCiId)
        {
            RelationshipType = input.RelationshipType
        };
        await _repository.InsertAsync(entity, autoSave: true);

        var target = await _configurationItemRepository.FindAsync(input.TargetCiId);
        return new ConfigurationItemRelationshipDto
        {
            Id = entity.Id,
            SourceCiId = entity.SourceCiId,
            TargetCiId = entity.TargetCiId,
            RelationshipType = entity.RelationshipType,
            TargetCiName = target?.Name
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Assets.Edit);
        await _repository.DeleteAsync(id);
    }
}
