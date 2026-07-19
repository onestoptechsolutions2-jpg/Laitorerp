using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog;

[Authorize(Policy = ErpPermissions.Catalog.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ProductAppService _productAppService;
    private readonly ProductCategoryAppService _productCategoryAppService;

    public IndexModel(ProductAppService productAppService, ProductCategoryAppService productCategoryAppService)
    {
        _productAppService = productAppService;
        _productCategoryAppService = productCategoryAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<ProductDto> Products { get; set; } = Array.Empty<ProductDto>();
    public List<SelectListItem> CategoryOptions { get; set; } = new();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Create);
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Edit);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var categories = await _productCategoryAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
        CategoryOptions = new List<SelectListItem> { new(L["AllCategories"], "") };
        CategoryOptions.AddRange(categories.Items.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));

        var result = await _productAppService.GetListAsync(new GetProductListInput
        {
            Filter = Filter,
            CategoryId = CategoryId,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Products = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _productAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
