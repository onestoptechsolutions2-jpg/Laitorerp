using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Accounting.FixedAssets;

[Authorize(Policy = ErpPermissions.FixedAssets.Default)]
public class DetailModel : AbpPageModel
{
    private readonly FixedAssetAppService _fixedAssetAppService;
    private readonly DepreciationAppService _depreciationAppService;
    private readonly IRepository<DepreciationEntry, Guid> _depreciationEntryRepository;

    public DetailModel(
        FixedAssetAppService fixedAssetAppService,
        DepreciationAppService depreciationAppService,
        IRepository<DepreciationEntry, Guid> depreciationEntryRepository)
    {
        _fixedAssetAppService = fixedAssetAppService;
        _depreciationAppService = depreciationAppService;
        _depreciationEntryRepository = depreciationEntryRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public FixedAssetDto Asset { get; set; } = null!;
    public IReadOnlyList<DepreciationEntry> Schedule { get; set; } = Array.Empty<DepreciationEntry>();
    public bool CanEdit { get; set; }

    [BindProperty]
    public DateTime PeriodMonth { get; set; } = DateTime.Today;

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.FixedAssets.Edit);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostRunDepreciationAsync()
    {
        try
        {
            await _depreciationAppService.RunDepreciationAsync(Id, PeriodMonth);
        }
        catch (Volo.Abp.UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id = Id });
    }

    private async Task LoadAsync()
    {
        Asset = await _fixedAssetAppService.GetAsync(Id);
        Schedule = (await _depreciationEntryRepository.GetListAsync(x => x.FixedAssetId == Id))
            .OrderByDescending(x => x.PeriodDate)
            .ToList();
    }
}
