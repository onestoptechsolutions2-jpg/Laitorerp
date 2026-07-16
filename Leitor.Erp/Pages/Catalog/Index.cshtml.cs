using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog;

[Authorize(Policy = ErpPermissions.Catalog.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ProductAppService _productAppService;

    public IndexModel(ProductAppService productAppService)
    {
        _productAppService = productAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();

    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Create);
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Edit);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Delete);

        var result = await _productAppService.GetListAsync(new GetProductListInput
        {
            Filter = Filter,
            MaxResultCount = 1000
        });

        Products = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _productAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter });
    }
}
