using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.PriceLists;

[Authorize(Policy = ErpPermissions.Catalog.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly PriceListAppService _priceListAppService;

    public CreateModel(PriceListAppService priceListAppService)
    {
        _priceListAppService = priceListAppService;
    }

    [BindProperty]
    public CreateUpdatePriceListDto PriceList { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var priceList = await _priceListAppService.CreateAsync(PriceList);
        return RedirectToPage("./Detail", new { id = priceList.Id });
    }
}
