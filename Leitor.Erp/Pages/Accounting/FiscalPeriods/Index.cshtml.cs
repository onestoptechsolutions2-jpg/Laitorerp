using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.FiscalPeriods;

[Authorize(Policy = ErpPermissions.FiscalPeriods.Default)]
public class IndexModel : AbpPageModel
{
    private readonly FiscalPeriodAppService _fiscalPeriodAppService;

    public IndexModel(FiscalPeriodAppService fiscalPeriodAppService)
    {
        _fiscalPeriodAppService = fiscalPeriodAppService;
    }

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.Today.Year;

    public FiscalPeriodGridDto Grid { get; set; } = null!;
    public bool CanManage { get; set; }

    public async Task OnGetAsync()
    {
        CanManage = await AuthorizationService.IsGrantedAsync(ErpPermissions.FiscalPeriods.Manage);
        Grid = await _fiscalPeriodAppService.GetGridAsync(Year);
    }

    public async Task<IActionResult> OnPostToggleAsync(int month, bool locked)
    {
        await _fiscalPeriodAppService.ToggleAsync(Year, month, locked);
        return RedirectToPage(new { Year });
    }

    public async Task<IActionResult> OnPostCloseYearAsync()
    {
        await _fiscalPeriodAppService.CloseYearAsync(Year);
        return RedirectToPage(new { Year });
    }
}
