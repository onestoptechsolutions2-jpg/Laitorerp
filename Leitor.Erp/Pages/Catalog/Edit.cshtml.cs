using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog;

[Authorize(Policy = ErpPermissions.Catalog.Edit)]
public class EditModel : AbpPageModel
{
    private readonly ProductAppService _productAppService;

    public EditModel(ProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateProductDto Product { get; set; } = new();

    public async Task OnGetAsync()
    {
        var product = await _productAppService.GetAsync(Id);
        Product = new CreateUpdateProductDto
        {
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Type = product.Type,
            UnitPrice = product.UnitPrice,
            IsActive = product.IsActive
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _productAppService.UpdateAsync(Id, Product);
        return RedirectToPage("./Index");
    }
}
