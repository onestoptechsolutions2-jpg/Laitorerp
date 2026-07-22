using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Budgets;

[Authorize(Policy = ErpPermissions.Budgets.Default)]
public class IndexModel : AbpPageModel
{
    private readonly BudgetAppService _budgetAppService;

    public IndexModel(BudgetAppService budgetAppService)
    {
        _budgetAppService = budgetAppService;
    }

    [BindProperty(SupportsGet = true)]
    public int FiscalYear { get; set; } = DateTime.Today.Year;

    public BudgetGridDto Grid { get; set; } = new();
    public bool CanEdit { get; set; }

    [BindProperty]
    public SaveBudgetGridDto Save { get; set; } = new();

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Budgets.Edit);
        Grid = await _budgetAppService.GetGridAsync(FiscalYear);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _budgetAppService.SaveGridAsync(Save);
        return RedirectToPage(new { FiscalYear = Save.FiscalYear });
    }
}
