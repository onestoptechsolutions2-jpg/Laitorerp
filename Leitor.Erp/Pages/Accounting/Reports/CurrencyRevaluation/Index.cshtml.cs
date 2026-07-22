using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Reports.CurrencyRevaluation;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly CurrencyRevaluationAppService _currencyRevaluationAppService;

    public IndexModel(CurrencyRevaluationAppService currencyRevaluationAppService)
    {
        _currencyRevaluationAppService = currencyRevaluationAppService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime AsOfDate { get; set; } = DateTime.Today;

    public CurrencyRevaluationPreviewDto Preview { get; set; } = null!;
    public bool CanPost { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        CanPost = await AuthorizationService.IsGrantedAsync(ErpPermissions.Accounting.Create);
        Preview = await _currencyRevaluationAppService.GetPreviewAsync(AsOfDate);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            await _currencyRevaluationAppService.PostRevaluationAsync(AsOfDate);
            SuccessMessage = "Revaluation entry posted.";
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { AsOfDate });
    }
}
