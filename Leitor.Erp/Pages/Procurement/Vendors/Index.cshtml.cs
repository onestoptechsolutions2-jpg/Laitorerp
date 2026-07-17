using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Procurement.Vendors;

[Authorize(Policy = ErpPermissions.Vendors.Default)]
public class IndexModel : AbpPageModel
{
    private readonly VendorAppService _vendorAppService;

    public IndexModel(VendorAppService vendorAppService)
    {
        _vendorAppService = vendorAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<VendorDto> Vendors { get; set; } = Array.Empty<VendorDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Vendors.Create);
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Vendors.Edit);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Vendors.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _vendorAppService.GetListAsync(new GetVendorListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Vendors = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _vendorAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
