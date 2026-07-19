using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.Categories;

[Authorize(Policy = ErpPermissions.Catalog.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ProductCategoryAppService _productCategoryAppService;

    public IndexModel(ProductCategoryAppService productCategoryAppService)
    {
        _productCategoryAppService = productCategoryAppService;
    }

    public IReadOnlyList<ProductCategoryDto> Categories { get; set; } = Array.Empty<ProductCategoryDto>();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Edit);

        var result = await _productCategoryAppService.GetListAsync(new PagedAndSortedResultRequestDto
        {
            MaxResultCount = 1000
        });
        Categories = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _productCategoryAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
