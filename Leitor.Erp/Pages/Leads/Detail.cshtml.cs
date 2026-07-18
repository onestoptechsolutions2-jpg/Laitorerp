using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Leads;

[Authorize(Policy = ErpPermissions.Leads.Default)]
public class DetailModel : AbpPageModel
{
    private readonly LeadAppService _leadAppService;
    private readonly LeadTouchAppService _leadTouchAppService;

    public DetailModel(LeadAppService leadAppService, LeadTouchAppService leadTouchAppService)
    {
        _leadAppService = leadAppService;
        _leadTouchAppService = leadTouchAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public LeadDto Lead { get; set; } = null!;
    public IReadOnlyList<LeadTouchDto> Touches { get; set; } = Array.Empty<LeadTouchDto>();

    // Derived per-channel status (latest touch per channel) - computed inline from the loaded
    // touch list, not a second stored field, so the log stays the single source of truth.
    public IReadOnlyList<LeadTouchDto> ChannelStatus { get; set; } = Array.Empty<LeadTouchDto>();

    [BindProperty]
    public CreateLeadTouchDto NewTouch { get; set; } = new();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Lead = await _leadAppService.GetAsync(Id);

        var touches = await _leadTouchAppService.GetListAsync(new GetLeadTouchListInput
        {
            LeadId = Id,
            MaxResultCount = 1000
        });
        Touches = touches.Items;

        ChannelStatus = Touches
            .GroupBy(x => x.Channel)
            .Select(g => g.OrderByDescending(x => x.TouchedAt).First())
            .OrderBy(x => x.Channel)
            .ToList();
    }

    public async Task<IActionResult> OnPostConvertToCustomerAsync()
    {
        var customerId = await _leadAppService.ConvertToCustomerAsync(Id);
        return RedirectToPage("/Customers/Detail", new { id = customerId });
    }

    public async Task<IActionResult> OnPostAddTouchAsync()
    {
        NewTouch.LeadId = Id;
        await _leadTouchAppService.CreateAsync(NewTouch);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteTouchAsync(Guid touchId)
    {
        await _leadTouchAppService.DeleteAsync(touchId);
        return RedirectToPage(new { id = Id });
    }
}
