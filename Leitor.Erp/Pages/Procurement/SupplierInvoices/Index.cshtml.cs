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

namespace Leitor.Erp.Pages.Procurement.SupplierInvoices;

[Authorize(Policy = ErpPermissions.Procurement.Default)]
public class IndexModel : AbpPageModel
{
    private readonly SupplierInvoiceAppService _supplierInvoiceAppService;

    public IndexModel(SupplierInvoiceAppService supplierInvoiceAppService)
    {
        _supplierInvoiceAppService = supplierInvoiceAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<SupplierInvoiceDto> SupplierInvoices { get; set; } = Array.Empty<SupplierInvoiceDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Procurement.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Procurement.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _supplierInvoiceAppService.GetListAsync(new GetSupplierInvoiceListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        SupplierInvoices = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _supplierInvoiceAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
