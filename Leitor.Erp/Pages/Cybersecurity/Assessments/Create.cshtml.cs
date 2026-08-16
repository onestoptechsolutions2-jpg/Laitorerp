using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Cybersecurity;
using Leitor.Erp.Services.Dtos.Cybersecurity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Cybersecurity.Assessments;

[Authorize(Policy = ErpPermissions.Cybersecurity.Create)]
public class CreateModel : AbpPageModel
{
    private readonly SecurityAssessmentAppService _securityAssessmentAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        SecurityAssessmentAppService securityAssessmentAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IFeatureChecker featureChecker)
    {
        _securityAssessmentAppService = securityAssessmentAppService;
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateSecurityAssessmentDto Assessment { get; set; } = new()
    {
        ScheduledDate = DateTime.Today
    };

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.Cybersecurity))
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

        var assessment = await _securityAssessmentAppService.CreateAsync(Assessment);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = assessment.Id }) });
        }

        return RedirectToPage("./Detail", new { id = assessment.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }
}
