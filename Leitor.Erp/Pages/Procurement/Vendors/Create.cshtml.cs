using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Procurement.Vendors;

[Authorize(Policy = ErpPermissions.Vendors.Create)]
public class CreateModel : AbpPageModel
{
    private readonly VendorAppService _vendorAppService;
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(VendorAppService vendorAppService, IRepository<TaxRate, Guid> taxRateRepository, IFeatureChecker featureChecker)
    {
        _vendorAppService = vendorAppService;
        _taxRateRepository = taxRateRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateVendorDto Vendor { get; set; } = new();

    public List<SelectListItem> WithholdingTaxRateOptions { get; set; } = new();
    public bool CanUseTaxCompliance { get; set; }

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _vendorAppService.CreateAsync(Vendor);
        return RedirectToPage("./Index");
    }

    private async Task LoadOptionsAsync()
    {
        CanUseTaxCompliance = await _featureChecker.IsEnabledAsync(ErpFeatures.TaxCompliance);
        if (!CanUseTaxCompliance)
        {
            return;
        }

        var rates = await _taxRateRepository.GetListAsync(x => x.TaxType == TaxType.WithholdingTax);
        WithholdingTaxRateOptions = new List<SelectListItem> { new(L["None"], "") };
        WithholdingTaxRateOptions.AddRange(
            rates.OrderBy(x => x.Name).Select(x => new SelectListItem($"{x.Name} ({x.Percent:N1}%)", x.Id.ToString()))
        );
    }
}
