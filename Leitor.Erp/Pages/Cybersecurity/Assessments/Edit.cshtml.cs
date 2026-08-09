using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Features;
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

[Authorize(Policy = ErpPermissions.Cybersecurity.Edit)]
public class EditModel : AbpPageModel
{
    private readonly SecurityAssessmentAppService _securityAssessmentAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(
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

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateSecurityAssessmentDto Assessment { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.Cybersecurity))
        {
            return NotFound();
        }

        var assessment = await _securityAssessmentAppService.GetAsync(Id);
        Assessment = new CreateUpdateSecurityAssessmentDto
        {
            CustomerId = assessment.CustomerId,
            Title = assessment.Title,
            Type = assessment.Type,
            Status = assessment.Status,
            ScheduledDate = assessment.ScheduledDate,
            ConductedByUserId = assessment.ConductedByUserId,
            RiskRating = assessment.RiskRating,
            Findings = assessment.Findings,
            Recommendations = assessment.Recommendations,
            FollowUpDate = assessment.FollowUpDate
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

        await _securityAssessmentAppService.UpdateAsync(Id, Assessment);
        return RedirectToPage("./Detail", new { id = Id });
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
