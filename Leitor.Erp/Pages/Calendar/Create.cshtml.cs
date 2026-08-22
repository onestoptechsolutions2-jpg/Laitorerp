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

[Authorize(Policy = ErpPermissions.Calendar.Create)]
public class CreateModel : AbpPageModel
{
    private readonly CalendarEventAppService _calendarEventAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Agent, Guid> _agentRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
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
    public DateTime? StartDate { get; set; }

    // StartDate is [Required] on the DTO. The FullCalendar dateClick path (Calendar/Index.cshtml)
    // always supplies ?StartDate=... so OnGetAsync overrides this below, but the plain "+ New
    // Calendar Event" header button links here with no query string at all - without this default,
    // that path renders a blank/zero date picker, and a user who doesn't notice they need to pick
    // one gets a silent [Required] validation failure: the overlay redisplays the same-looking
    // form with no obvious change, indistinguishable from "clicking Save does nothing".
    [BindProperty]
    public CreateUpdateCalendarEventDto EventInput { get; set; } = new()
    {
        StartDate = DateTime.Now
    };

    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> AgentOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.Calendar))
        {
            return NotFound();
        }

        if (StartDate.HasValue)
        {
            EventInput.StartDate = StartDate.Value;
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

        await _calendarEventAppService.CreateAsync(EventInput);

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
