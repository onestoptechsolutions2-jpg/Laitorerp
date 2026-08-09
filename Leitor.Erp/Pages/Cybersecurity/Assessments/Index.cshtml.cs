using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Cybersecurity;
using Leitor.Erp.Services.Dtos.Cybersecurity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Cybersecurity.Assessments;

[Authorize(Policy = ErpPermissions.Cybersecurity.Default)]
public class IndexModel : AbpPageModel
{
    private readonly SecurityAssessmentAppService _securityAssessmentAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(SecurityAssessmentAppService securityAssessmentAppService, IFeatureChecker featureChecker)
    {
        _securityAssessmentAppService = securityAssessmentAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<SecurityAssessmentDto> Assessments { get; set; } = Array.Empty<SecurityAssessmentDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.Cybersecurity))
        {
            return NotFound();
        }

        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Cybersecurity.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Cybersecurity.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _securityAssessmentAppService.GetListAsync(new GetSecurityAssessmentListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Assessments = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _securityAssessmentAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
