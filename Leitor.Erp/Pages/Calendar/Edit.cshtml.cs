using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Calendar;
using Leitor.Erp.Services.Dtos.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Calendar;

[Authorize(Policy = ErpPermissions.Calendar.Edit)]
public class EditModel : AbpPageModel
{
    private readonly CalendarEventAppService _calendarEventAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(
        CalendarEventAppService calendarEventAppService,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Agent, Guid> agentRepository,
        IFeatureChecker featureChecker)
    {
        _calendarEventAppService = calendarEventAppService;
        _identityUserRepository = identityUserRepository;
        _agentRepository = agentRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateCalendarEventDto EventInput { get; set; } = new();

    public bool CanDelete { get; set; }
    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> AgentOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.Calendar))
        {
            return NotFound();
        }

        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Calendar.Delete);

        var calendarEvent = await _calendarEventAppService.GetAsync(Id);
        EventInput = new CreateUpdateCalendarEventDto
        {
            Title = calendarEvent.Title,
            Description = calendarEvent.Description,
            StartDate = calendarEvent.StartDate,
            EndDate = calendarEvent.EndDate,
            AssignedToUserId = calendarEvent.AssignedToUserId,
            AgentId = calendarEvent.AgentId
        };

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

        await _calendarEventAppService.UpdateAsync(Id, EventInput);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("/Calendar/Index") });
        }

        return RedirectToPage("/Calendar/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await _calendarEventAppService.DeleteAsync(Id);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("/Calendar/Index") });
        }

        return RedirectToPage("/Calendar/Index");
    }

    private async Task LoadOptionsAsync()
    {
        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString())));

        var agents = await _agentRepository.GetListAsync();
        AgentOptions = new List<SelectListItem> { new(L["None"], "") };
        AgentOptions.AddRange(agents.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));
    }
}
