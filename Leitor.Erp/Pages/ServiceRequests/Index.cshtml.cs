using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.ServiceRequests;
using Leitor.Erp.Services.ServiceRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.ServiceRequests;

[Authorize(Policy = ErpPermissions.ServiceRequests.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ServiceRequestAppService _serviceRequestAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(ServiceRequestAppService serviceRequestAppService, IFeatureChecker featureChecker)
    {
        _serviceRequestAppService = serviceRequestAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<ServiceRequestDto> ServiceRequests { get; set; } = Array.Empty<ServiceRequestDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ServiceRequestManagement))
        {
            return NotFound();
        }

        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.ServiceRequests.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.ServiceRequests.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _serviceRequestAppService.GetListAsync(new GetServiceRequestListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        ServiceRequests = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _serviceRequestAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
