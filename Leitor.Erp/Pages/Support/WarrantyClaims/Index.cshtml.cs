using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Support.WarrantyClaims;

[Authorize(Policy = ErpPermissions.Support.Default)]
public class IndexModel : AbpPageModel
{
    private readonly WarrantyClaimAppService _warrantyClaimAppService;

    public IndexModel(WarrantyClaimAppService warrantyClaimAppService)
    {
        _warrantyClaimAppService = warrantyClaimAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<WarrantyClaimDto> WarrantyClaims { get; set; } = Array.Empty<WarrantyClaimDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Support.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Support.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _warrantyClaimAppService.GetListAsync(new GetWarrantyClaimListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        WarrantyClaims = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _warrantyClaimAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
