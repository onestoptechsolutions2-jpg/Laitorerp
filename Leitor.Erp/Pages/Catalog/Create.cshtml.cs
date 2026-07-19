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
    private readonly ProductCategoryAppService _productCategoryAppService;

    public CreateModel(
        ProductAppService productAppService,
        TaxRateAppService taxRateAppService,
        ProductCategoryAppService productCategoryAppService)
    {
        _productAppService = productAppService;
        _taxRateAppService = taxRateAppService;
        _productCategoryAppService = productCategoryAppService;
    }

    [BindProperty]
    public CreateUpdateProductDto Product { get; set; } = new();

    public List<SelectListItem> TaxRateOptions { get; set; } = new();
    public List<SelectListItem> CategoryOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
    }

    private async Task LoadOptionsAsync()
    {
        var taxRates = await _taxRateAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        TaxRateOptions = taxRates.Items.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        var categories = await _productCategoryAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        CategoryOptions = categories.Items.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _productAppService.CreateAsync(Product);
        return RedirectToPage("./Index");
    }
}
