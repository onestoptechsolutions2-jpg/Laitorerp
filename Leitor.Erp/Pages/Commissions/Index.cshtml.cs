using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Commissions;

[Authorize(Policy = ErpPermissions.Partners.Default)]
public class IndexModel : AbpPageModel
{
    private readonly CommissionAppService _commissionAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(CommissionAppService commissionAppService, IFeatureChecker featureChecker)
    {
        _commissionAppService = commissionAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? OpportunityId { get; set; }

    [BindProperty(SupportsGet = true)]
    public CommissionStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<CommissionDto> Items { get; set; } = Array.Empty<CommissionDto>();
    public PaginationModel Pagination { get; set; } = new();
    public bool CanEdit { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.PartnerCommission))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Partners.Edit);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _commissionAppService.GetListAsync(new GetCommissionListInput
        {
            OpportunityId = OpportunityId,
            Status = Status,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Items = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostMarkPaidAsync(Guid id)
    {
        await _commissionAppService.MarkPaidAsync(id);
        return RedirectToPage(new { OpportunityId, Status, PageIndex });
    }
}
