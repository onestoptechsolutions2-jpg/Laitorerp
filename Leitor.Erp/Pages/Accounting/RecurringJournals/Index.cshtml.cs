using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.RecurringJournals;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly RecurringJournalTemplateAppService _templateAppService;

    public IndexModel(RecurringJournalTemplateAppService templateAppService)
    {
        _templateAppService = templateAppService;
    }

    public List<RecurringJournalTemplateDto> Templates { get; set; } = new();
    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Accounting.Edit);
        Templates = await _templateAppService.GetListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _templateAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
