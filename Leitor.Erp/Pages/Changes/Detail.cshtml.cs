using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Changes;

[Authorize(Policy = ErpPermissions.Changes.Default)]
public class DetailModel : AbpPageModel
{
    private readonly ChangeRequestAppService _changeRequestAppService;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        ChangeRequestAppService changeRequestAppService,
        IRepository<Ticket, Guid> ticketRepository,
        IFeatureChecker featureChecker)
    {
        _changeRequestAppService = changeRequestAppService;
        _ticketRepository = ticketRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ChangeRequestDto Change { get; set; } = null!;
    public string? TicketNumber { get; set; }

    public bool CanApprove { get; set; }
    public bool CanEdit { get; set; }

    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ChangeEnablement))
        {
            return NotFound();
        }

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync()
    {
        try
        {
            await _changeRequestAppService.ApproveAsync(Id);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostRejectAsync(string reason)
    {
        try
        {
            await _changeRequestAppService.RejectAsync(Id, reason);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostCompleteAsync()
    {
        try
        {
            await _changeRequestAppService.CompleteAsync(Id);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostRollBackAsync(string notes)
    {
        try
        {
            await _changeRequestAppService.RollBackAsync(Id, notes);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostReviewPostImplementationAsync()
    {
        try
        {
            await _changeRequestAppService.ReviewPostImplementationAsync(Id);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Id });
    }

    private async Task LoadAsync()
    {
        Change = await _changeRequestAppService.GetAsync(Id);

        if (Change.TicketId.HasValue)
        {
            var ticket = await _ticketRepository.FindAsync(Change.TicketId.Value);
            TicketNumber = ticket?.TicketNumber;
        }

        CanApprove = await AuthorizationService.IsGrantedAsync(ErpPermissions.Changes.Approve);
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Changes.Edit);
    }
}
