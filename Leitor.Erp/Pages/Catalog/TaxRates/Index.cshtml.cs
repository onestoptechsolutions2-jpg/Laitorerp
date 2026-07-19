using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.TaxRates;

[Authorize(Policy = ErpPermissions.Catalog.Default)]
public class IndexModel : AbpPageModel
{
    private readonly TaxRateAppService _taxRateAppService;

    public IndexModel(TaxRateAppService taxRateAppService)
    {
        _taxRateAppService = taxRateAppService;
    }

    public IReadOnlyList<TaxRateDto> TaxRates { get; set; } = Array.Empty<TaxRateDto>();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Edit);

        var result = await _taxRateAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            MaxResultCount = 1000
        });
        TaxRates = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _taxRateAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
