using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.Reports.VendorStatement;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly StatementAppService _statementAppService;

    public IndexModel(StatementAppService statementAppService)
    {
        _statementAppService = statementAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid VendorId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, 1, 1);

    [BindProperty(SupportsGet = true)]
    public DateTime ToDate { get; set; } = DateTime.Today;

    public StatementDto Statement { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Statement = await _statementAppService.GetVendorStatementAsync(VendorId, FromDate, ToDate);
    }
}
