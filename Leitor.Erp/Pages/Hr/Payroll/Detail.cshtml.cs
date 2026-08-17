using System;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Hr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Hr.Payroll;

[Authorize(Policy = ErpPermissions.Payroll.Default)]
public class DetailModel : AbpPageModel
{
    private readonly PayrollRunAppService _payrollRunAppService;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(PayrollRunAppService payrollRunAppService, IFeatureChecker featureChecker)
    {
        _payrollRunAppService = payrollRunAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PayrollRunDto Run { get; set; } = null!;
    public bool CanRun { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.HumanResources))
        {
            return NotFound();
        }

        CanRun = await AuthorizationService.IsGrantedAsync(ErpPermissions.Payroll.Run);
        Run = await _payrollRunAppService.GetAsync(Id);
        return Page();
    }

    public async Task<IActionResult> OnPostPostAsync()
    {
        try
        {
            await _payrollRunAppService.PostAsync(Id);
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id = Id });
    }
}
