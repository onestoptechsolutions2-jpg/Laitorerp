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

namespace Leitor.Erp.Pages.Governance.Escalations;

[Authorize(Policy = ErpPermissions.Escalations.Default)]
public class IndexModel : AbpPageModel
{
    private readonly EscalationItemAppService _escalationItemAppService;

    public IndexModel(EscalationItemAppService escalationItemAppService)
    {
        _escalationItemAppService = escalationItemAppService;
    }

    [BindProperty(SupportsGet = true)]
    public EscalationItemStatus? Status { get; set; } = EscalationItemStatus.Pending;

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<EscalationItemDto> Items { get; set; } = Array.Empty<EscalationItemDto>();
    public PaginationModel Pagination { get; set; } = new();

    // Decide-ability is per-row (each item's own RequiredPermission), not one page-wide flag like
    // DeletionApprovals' CanDecide - see EscalationItemAppService.CanDecideAsync.
    public Dictionary<Guid, bool> CanDecideById { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _escalationItemAppService.GetListAsync(new GetEscalationItemListInput
        {
            Status = Status,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Items = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };

        foreach (var item in Items)
        {
            CanDecideById[item.Id] = await _escalationItemAppService.CanDecideAsync(item.RequiredPermission);
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(Guid id)
    {
        await _escalationItemAppService.ApproveAsync(id);
        return RedirectToPage(new { Status, PageIndex });
    }

    public async Task<IActionResult> OnPostRejectAsync(Guid id, string? notes)
    {
        await _escalationItemAppService.RejectAsync(id, notes);
        return RedirectToPage(new { Status, PageIndex });
    }

    // Small per-page switch, same tradeoff DeletionApprovals' own GetEntityDetailUrl already
    // accepts - fine while there are only 1-2 consumers (Quote/Order margin overrides).
    public static string GetEntityDetailUrl(string entityType, Guid entityId)
    {
        return entityType switch
        {
            "Quote" => $"/Sales/Quotes/Detail?id={entityId}",
            "Order" => $"/Sales/Orders/Detail?id={entityId}",
            _ => "#"
        };
    }
}
