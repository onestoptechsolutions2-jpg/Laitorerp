using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Dtos.Assets;
using Leitor.Erp.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Assets;

[Authorize(Policy = ErpPermissions.Assets.Default)]
public class DetailModel : AbpPageModel
{
    private readonly ConfigurationItemAppService _configurationItemAppService;
    private readonly ConfigurationItemRelationshipAppService _relationshipAppService;
    private readonly AssetCredentialAppService _assetCredentialAppService;
    private readonly IRepository<Entities.Assets.ConfigurationItem, Guid> _configurationItemRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        ConfigurationItemAppService configurationItemAppService,
        ConfigurationItemRelationshipAppService relationshipAppService,
        AssetCredentialAppService assetCredentialAppService,
        IRepository<Entities.Assets.ConfigurationItem, Guid> configurationItemRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IFeatureChecker featureChecker)
    {
        _configurationItemAppService = configurationItemAppService;
        _relationshipAppService = relationshipAppService;
        _assetCredentialAppService = assetCredentialAppService;
        _configurationItemRepository = configurationItemRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ConfigurationItemDto Item { get; set; } = null!;
    public IReadOnlyList<ConfigurationItemRelationshipDto> Relationships { get; set; } = Array.Empty<ConfigurationItemRelationshipDto>();
    public IReadOnlyList<AssetCredentialDto> Credentials { get; set; } = Array.Empty<AssetCredentialDto>();
    public List<SelectListItem> TargetCiOptions { get; set; } = new();

    [BindProperty]
    public CreateConfigurationItemRelationshipDto NewRelationship { get; set; } = new();

    [BindProperty]
    public CreateUpdateAssetCredentialDto NewCredential { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool CanRevealCredentials { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.AssetManagement))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Assets.Edit);
        CanRevealCredentials = await AuthorizationService.IsGrantedAsync(ErpPermissions.Assets.RevealCredentials);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "ConfigurationItem", Id);
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Item = await _configurationItemAppService.GetAsync(Id);
        Relationships = await _relationshipAppService.GetListAsync(Id);

        var credentials = await _assetCredentialAppService.GetListAsync(new GetAssetCredentialListInput
        {
            ConfigurationItemId = Id,
            MaxResultCount = 1000
        });
        Credentials = credentials.Items;

        var others = await _configurationItemRepository.GetListAsync(x => x.Id != Id);
        TargetCiOptions = others.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    }

    public async Task<IActionResult> OnPostAddRelationshipAsync()
    {
        NewRelationship.SourceCiId = Id;
        if (NewRelationship.TargetCiId != Guid.Empty)
        {
            await _relationshipAppService.CreateAsync(NewRelationship);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteRelationshipAsync(Guid relationshipId)
    {
        await _relationshipAppService.DeleteAsync(relationshipId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddCredentialAsync()
    {
        NewCredential.ConfigurationItemId = Id;
        await _assetCredentialAppService.CreateAsync(NewCredential);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteCredentialAsync(Guid credentialId)
    {
        await _assetCredentialAppService.DeleteAsync(credentialId);
        return RedirectToPage(new { id = Id });
    }

    // AJAX-only (see the Credentials card's inline script) - GET rather than POST since it changes
    // nothing server-side beyond the WorkflowStageLog audit row AssetCredentialAppService.RevealAsync
    // itself writes; same "read that has a logged side effect" shape as an ABP-audited GET already
    // has by default. Never returns anything if the caller isn't Assets.RevealCredentials-granted -
    // enforced inside RevealAsync, not just by hiding the button.
    public async Task<IActionResult> OnGetRevealCredentialAsync(Guid credentialId)
    {
        var secret = await _assetCredentialAppService.RevealAsync(credentialId);
        return new JsonResult(secret);
    }
}
