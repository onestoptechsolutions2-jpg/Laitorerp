using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog;

[Authorize(Policy = ErpPermissions.Catalog.Create)]
public class CreateModel : AbpPageModel
{
    private readonly ProductAppService _productAppService;
    private readonly TaxRateAppService _taxRateAppService;

    public CreateModel(ProductAppService productAppService, TaxRateAppService taxRateAppService)
    {
        _productAppService = productAppService;
        _taxRateAppService = taxRateAppService;
    }

    [BindProperty]
    public CreateUpdateProductDto Product { get; set; } = new();

    public List<SelectListItem> TaxRateOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadTaxRateOptionsAsync();
    }

    private async Task LoadTaxRateOptionsAsync()
    {
        var taxRates = await _taxRateAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        TaxRateOptions = taxRates.Items.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadTaxRateOptionsAsync();
            return Page();
        }

        await _productAppService.CreateAsync(Product);
        return RedirectToPage("./Index");
    }
}
