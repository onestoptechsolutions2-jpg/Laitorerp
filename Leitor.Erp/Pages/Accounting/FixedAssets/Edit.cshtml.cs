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
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Accounting.FixedAssets;

[Authorize(Policy = ErpPermissions.FixedAssets.Edit)]
public class EditModel : AbpPageModel
{
    private readonly FixedAssetAppService _fixedAssetAppService;
    private readonly IRepository<Account, Guid> _accountRepository;

    public EditModel(FixedAssetAppService fixedAssetAppService, IRepository<Account, Guid> accountRepository)
    {
        _fixedAssetAppService = fixedAssetAppService;
        _accountRepository = accountRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateFixedAssetDto Asset { get; set; } = new();

    public List<SelectListItem> AccountOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        var asset = await _fixedAssetAppService.GetAsync(Id);
        Asset = new CreateUpdateFixedAssetDto
        {
            Name = asset.Name,
            Category = asset.Category,
            PurchaseDate = asset.PurchaseDate,
            PurchaseCost = asset.PurchaseCost,
            SalvageValue = asset.SalvageValue,
            UsefulLifeMonths = asset.UsefulLifeMonths,
            Status = asset.Status,
            AssetAccountId = asset.AssetAccountId,
            DepreciationExpenseAccountId = asset.DepreciationExpenseAccountId,
            AccumulatedDepreciationAccountId = asset.AccumulatedDepreciationAccountId
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _fixedAssetAppService.UpdateAsync(Id, Asset);
        return RedirectToPage("./Index");
    }

    private async Task LoadOptionsAsync()
    {
        var accounts = await _accountRepository.GetListAsync(x => x.IsActive);
        AccountOptions = accounts
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString()))
            .ToList();
    }
}
