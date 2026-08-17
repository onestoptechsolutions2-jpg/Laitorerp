using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Hr.Payroll.TaxBands;

[Authorize(Policy = ErpPermissions.Payroll.ManageRates)]
public class IndexModel : AbpPageModel
{
    private readonly PayeTaxBandAppService _payeTaxBandAppService;
    private readonly NssfTierAppService _nssfTierAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(
        PayeTaxBandAppService payeTaxBandAppService,
        NssfTierAppService nssfTierAppService,
        IFeatureChecker featureChecker)
    {
        _payeTaxBandAppService = payeTaxBandAppService;
        _nssfTierAppService = nssfTierAppService;
        _featureChecker = featureChecker;
    }

    public IReadOnlyList<PayeTaxBandDto> PayeTaxBands { get; set; } = Array.Empty<PayeTaxBandDto>();
    public IReadOnlyList<NssfTierDto> NssfTiers { get; set; } = Array.Empty<NssfTierDto>();

    [BindProperty]
    public CreateUpdatePayeTaxBandDto NewPayeTaxBand { get; set; } = new();

    [BindProperty]
    public CreateUpdateNssfTierDto NewNssfTier { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddPayeTaxBandAsync()
    {
        await _payeTaxBandAppService.CreateAsync(NewPayeTaxBand);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeletePayeTaxBandAsync(Guid id)
    {
        await _payeTaxBandAppService.DeleteAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAddNssfTierAsync()
    {
        await _nssfTierAppService.CreateAsync(NewNssfTier);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteNssfTierAsync(Guid id)
    {
        await _nssfTierAppService.DeleteAsync(id);
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var bands = await _payeTaxBandAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        PayeTaxBands = bands.Items;

        var tiers = await _nssfTierAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        NssfTiers = tiers.Items;
    }
}
