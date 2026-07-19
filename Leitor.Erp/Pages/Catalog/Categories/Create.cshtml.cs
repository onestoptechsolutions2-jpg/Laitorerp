using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.Categories;

[Authorize(Policy = ErpPermissions.Catalog.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly ProductCategoryAppService _productCategoryAppService;

    public CreateModel(ProductCategoryAppService productCategoryAppService)
    {
        _productCategoryAppService = productCategoryAppService;
    }

    [BindProperty]
    public CreateUpdateProductCategoryDto Category { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _productCategoryAppService.CreateAsync(Category);
        return RedirectToPage("./Index");
    }
}
