using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Assets;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Security.Encryption;

namespace Leitor.Erp.Services.Assets;

[RequiresFeature(ErpFeatures.AssetManagement)]
public class AssetCredentialAppService :
    CrudAppService<AssetCredential, AssetCredentialDto, Guid, GetAssetCredentialListInput, CreateUpdateAssetCredentialDto>
{
    private readonly IStringEncryptionService _stringEncryptionService;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;

    public AssetCredentialAppService(
        IRepository<AssetCredential, Guid> repository,
        IStringEncryptionService stringEncryptionService,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository)
        : base(repository)
    {
        _stringEncryptionService = stringEncryptionService;
        _stageEventRepository = stageEventRepository;

        GetPolicyName = ErpPermissions.Assets.Default;
        GetListPolicyName = ErpPermissions.Assets.Default;
        CreatePolicyName = ErpPermissions.Assets.Edit;
        UpdatePolicyName = ErpPermissions.Assets.Edit;
        DeletePolicyName = ErpPermissions.Assets.Edit;
    }

    protected override async Task<IQueryable<AssetCredential>> CreateFilteredQueryAsync(GetAssetCredentialListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.ConfigurationItemId.HasValue, x => x.ConfigurationItemId == input.ConfigurationItemId!.Value);
    }

    // The one place a stored credential's plain value is ever readable. Separately gated from
    // Get/GetList (which return AssetCredentialDto - no value at all) behind
    // Assets.RevealCredentials, and every reveal is recorded via WorkflowStageLog the same way
    // Proposal/Order/Customer already log their own auditable business moments - "someone looked
    // at this client's password" is exactly that kind of moment, not just a property read.
    public async Task<AssetCredentialSecretDto> RevealAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Assets.RevealCredentials);

        var entity = await Repository.GetAsync(id);
        // IStringEncryptionService's null-in/null-out contract makes Decrypt return string? -
        // entity.EncryptedValue itself is a non-nullable string (see its own declaration), so the
        // result here can't actually be null.
        var value = _stringEncryptionService.Decrypt(entity.EncryptedValue)!;

        await WorkflowStageLog.RecordAsync(
            _stageEventRepository, GuidGenerator, CurrentUser, Clock,
            "AssetCredential", entity.Id, WorkflowStage.CredentialRevealed, notes: entity.Label);

        return new AssetCredentialSecretDto { Value = value };
    }

    protected override Task<AssetCredential> MapToEntityAsync(CreateUpdateAssetCredentialDto createInput)
    {
        var entity = new AssetCredential(GuidGenerator.Create(), createInput.ConfigurationItemId, createInput.Label);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateAssetCredentialDto updateInput, AssetCredential entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateAssetCredentialDto input, AssetCredential entity)
    {
        entity.ConfigurationItemId = input.ConfigurationItemId;
        entity.Label = input.Label;
        entity.CredentialType = input.CredentialType;
        entity.Username = input.Username;
        entity.Notes = input.Notes;
        // Same null-in/null-out contract as Decrypt above - input.Value is non-nullable.
        entity.EncryptedValue = _stringEncryptionService.Encrypt(input.Value)!;
    }
}
