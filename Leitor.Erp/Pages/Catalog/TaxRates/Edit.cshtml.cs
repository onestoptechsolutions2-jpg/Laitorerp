using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.TaxRates;

[Authorize(Policy = ErpPermissions.Catalog.Edit)]
public class EditModel : AbpPageModel
{
    private readonly TaxRateAppService _taxRateAppService;

    public EditModel(TaxRateAppService taxRateAppService)
    {
        _taxRateAppService = taxRateAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateTaxRateDto TaxRate { get; set; } = new();

    public async Task OnGetAsync()
    {
        var taxRate = await _taxRateAppService.GetAsync(Id);
        TaxRate = new CreateUpdateTaxRateDto
        {
            Name = taxRate.Name,
            Percent = taxRate.Percent,
            IsDefault = taxRate.IsDefault
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _taxRateAppService.UpdateAsync(Id, TaxRate);
        return RedirectToPage("./Index");
    }
}
