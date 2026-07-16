using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog;

[Authorize(Policy = ErpPermissions.Catalog.Create)]
public class CreateModel : AbpPageModel
{
    private readonly ProductAppService _productAppService;

    public CreateModel(ProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    [BindProperty]
    public CreateUpdateProductDto Product { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _productAppService.CreateAsync(Product);
        return RedirectToPage("./Index");
    }
}
