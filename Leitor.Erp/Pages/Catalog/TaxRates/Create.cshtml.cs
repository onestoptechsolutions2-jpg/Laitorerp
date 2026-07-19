using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.TaxRates;

[Authorize(Policy = ErpPermissions.Catalog.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly TaxRateAppService _taxRateAppService;

    public CreateModel(TaxRateAppService taxRateAppService)
    {
        _taxRateAppService = taxRateAppService;
    }

    [BindProperty]
    public CreateUpdateTaxRateDto TaxRate { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _taxRateAppService.CreateAsync(TaxRate);
        return RedirectToPage("./Index");
    }
}
