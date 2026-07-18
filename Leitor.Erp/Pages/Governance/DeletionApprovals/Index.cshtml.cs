using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Governance.DeletionApprovals;

[Authorize(Policy = ErpPermissions.DeletionApprovals.Default)]
public class IndexModel : AbpPageModel
{
    private readonly DeletionRequestAppService _deletionRequestAppService;

    public IndexModel(DeletionRequestAppService deletionRequestAppService)
    {
        _deletionRequestAppService = deletionRequestAppService;
    }

    [BindProperty(SupportsGet = true)]
    public DeletionRequestStatus? Status { get; set; } = DeletionRequestStatus.Pending;

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<DeletionRequestDto> Requests { get; set; } = Array.Empty<DeletionRequestDto>();
    public PaginationModel Pagination { get; set; } = new();
    public bool CanDecide { get; set; }

    public async Task OnGetAsync()
    {
        CanDecide = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _deletionRequestAppService.GetListAsync(new GetDeletionRequestListInput
        {
            Status = Status,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Requests = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        await _deletionRequestAppService.ApproveAsync(id);
        return RedirectToPage(new { Status, PageIndex });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id, string? notes)
    {
        await _deletionRequestAppService.RejectAsync(id, notes);
        return RedirectToPage(new { Status, PageIndex });
    }

    public static string GetEntityDetailUrl(string entityType, Guid entityId)
    {
        return entityType switch
        {
            "Customer" => $"/Customers/Detail?id={entityId}",
            "Vendor" => $"/Procurement/Vendors/Edit?id={entityId}",
            "Order" => $"/Sales/Orders/Detail?id={entityId}",
            "Invoice" => $"/Sales/Invoices/Detail?id={entityId}",
            "Ticket" => $"/Support/Tickets/Detail?id={entityId}",
            "FieldServiceJob" => $"/FieldService/Jobs/Detail?id={entityId}",
            "PurchaseOrder" => $"/Procurement/PurchaseOrders/Detail?id={entityId}",
            _ => "#"
        };
    }
}
