using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Assets;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Governance;
using Leitor.Erp.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Changes;

[Authorize(Policy = ErpPermissions.Changes.Create)]
public class CreateModel : AbpPageModel
{
    private readonly ChangeRequestAppService _changeRequestAppService;
    private readonly IRepository<ConfigurationItem, Guid> _configurationItemRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        ChangeRequestAppService changeRequestAppService,
        IRepository<ConfigurationItem, Guid> configurationItemRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IFeatureChecker featureChecker)
    {
        _changeRequestAppService = changeRequestAppService;
        _configurationItemRepository = configurationItemRepository;
        _ticketRepository = ticketRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateChangeRequestDto ChangeRequest { get; set; } = new();

    public List<SelectListItem> ConfigurationItemOptions { get; set; } = new();
    public List<SelectListItem> TicketOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ChangeEnablement))
        {
            return NotFound();
        }

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var change = await _changeRequestAppService.CreateAsync(ChangeRequest);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = change.Id }) });
        }

        return RedirectToPage("./Detail", new { id = change.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var configurationItems = await _configurationItemRepository.GetListAsync();
        ConfigurationItemOptions = configurationItems
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var tickets = await _ticketRepository.GetListAsync(x => x.Status != TicketStatus.Closed);
        TicketOptions = new List<SelectListItem> { new(L["None"], "") };
        TicketOptions.AddRange(tickets.OrderByDescending(x => x.CreationTime).Select(x => new SelectListItem($"{x.TicketNumber} - {x.Subject}", x.Id.ToString())));
    }
}
