using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.ServiceCatalog;
using Leitor.Erp.Services.ServiceCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.ServiceCatalog;

[Authorize(Policy = ErpPermissions.ServiceCatalog.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly ServiceCatalogItemAppService _serviceCatalogItemAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        ServiceCatalogItemAppService serviceCatalogItemAppService,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IFeatureChecker featureChecker)
    {
        _serviceCatalogItemAppService = serviceCatalogItemAppService;
        _identityUserRepository = identityUserRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateServiceCatalogItemDto Item { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ServiceCatalog))
        {
            return NotFound();
        }

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _serviceCatalogItemAppService.CreateAsync(Item);
        return RedirectToPage("./Index");
    }

    private async Task LoadOptionsAsync()
    {
        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }
}
