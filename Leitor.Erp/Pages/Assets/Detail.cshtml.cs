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
    private readonly IRepository<Entities.Assets.ConfigurationItem, Guid> _configurationItemRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        ConfigurationItemAppService configurationItemAppService,
        ConfigurationItemRelationshipAppService relationshipAppService,
        IRepository<Entities.Assets.ConfigurationItem, Guid> configurationItemRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IFeatureChecker featureChecker)
    {
        _configurationItemAppService = configurationItemAppService;
        _relationshipAppService = relationshipAppService;
        _configurationItemRepository = configurationItemRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ConfigurationItemDto Item { get; set; } = null!;
    public IReadOnlyList<ConfigurationItemRelationshipDto> Relationships { get; set; } = Array.Empty<ConfigurationItemRelationshipDto>();
    public List<SelectListItem> TargetCiOptions { get; set; } = new();

    [BindProperty]
    public CreateConfigurationItemRelationshipDto NewRelationship { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.AssetManagement))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Assets.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "ConfigurationItem", Id);
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Item = await _configurationItemAppService.GetAsync(Id);
        Relationships = await _relationshipAppService.GetListAsync(Id);

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
}
