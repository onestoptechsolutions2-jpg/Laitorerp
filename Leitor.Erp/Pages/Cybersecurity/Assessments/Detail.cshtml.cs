using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Cybersecurity;
using Leitor.Erp.Services.Dtos.Cybersecurity;
using Leitor.Erp.Services.Governance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Cybersecurity.Assessments;

[Authorize(Policy = ErpPermissions.Cybersecurity.Default)]
public class DetailModel : AbpPageModel
{
    private readonly SecurityAssessmentAppService _securityAssessmentAppService;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        SecurityAssessmentAppService securityAssessmentAppService,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IFeatureChecker featureChecker)
    {
        _securityAssessmentAppService = securityAssessmentAppService;
        _deletionRequestRepository = deletionRequestRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public SecurityAssessmentDto Assessment { get; set; } = null!;

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.Cybersecurity))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Cybersecurity.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "SecurityAssessment", Id);
        Assessment = await _securityAssessmentAppService.GetAsync(Id);
        return Page();
    }

    public async Task<IActionResult> OnPostSetStatusAsync(Entities.Cybersecurity.SecurityAssessmentStatus status)
    {
        var assessment = await _securityAssessmentAppService.GetAsync(Id);
        await _securityAssessmentAppService.UpdateAsync(Id, new CreateUpdateSecurityAssessmentDto
        {
            CustomerId = assessment.CustomerId,
            Title = assessment.Title,
            Type = assessment.Type,
            Status = status,
            ScheduledDate = assessment.ScheduledDate,
            ConductedByUserId = assessment.ConductedByUserId,
            RiskRating = assessment.RiskRating,
            Findings = assessment.Findings,
            Recommendations = assessment.Recommendations,
            FollowUpDate = assessment.FollowUpDate
        });

        return RedirectToPage(new { id = Id });
    }
}
