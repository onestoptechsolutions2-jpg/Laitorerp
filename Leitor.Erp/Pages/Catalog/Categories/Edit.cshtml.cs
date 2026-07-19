using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.Categories;

[Authorize(Policy = ErpPermissions.Catalog.Edit)]
public class EditModel : AbpPageModel
{
    private readonly ProductCategoryAppService _productCategoryAppService;

    public EditModel(ProductCategoryAppService productCategoryAppService)
    {
        _productCategoryAppService = productCategoryAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateProductCategoryDto Category { get; set; } = new();

    public async Task OnGetAsync()
    {
        var category = await _productCategoryAppService.GetAsync(Id);
        Category = new CreateUpdateProductCategoryDto
        {
            Name = category.Name
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _productCategoryAppService.UpdateAsync(Id, Category);
        return RedirectToPage("./Index");
    }
}
