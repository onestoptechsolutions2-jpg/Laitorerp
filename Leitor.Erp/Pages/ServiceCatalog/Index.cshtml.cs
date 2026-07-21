using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.ServiceCatalog;
using Leitor.Erp.Services.ServiceCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.ServiceCatalog;

[Authorize(Policy = ErpPermissions.ServiceCatalog.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ServiceCatalogItemAppService _serviceCatalogItemAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(ServiceCatalogItemAppService serviceCatalogItemAppService, IFeatureChecker featureChecker)
    {
        _serviceCatalogItemAppService = serviceCatalogItemAppService;
        _featureChecker = featureChecker;
    }

    public IReadOnlyList<ServiceCatalogItemDto> Items { get; set; } = Array.Empty<ServiceCatalogItemDto>();
    public bool CanEdit { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ServiceCatalog))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.ServiceCatalog.Edit);

        var result = await _serviceCatalogItemAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        Items = result.Items;
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _serviceCatalogItemAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
