using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.JournalEntries;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class DetailModel : AbpPageModel
{
    private readonly JournalEntryAppService _journalEntryAppService;

    public DetailModel(JournalEntryAppService journalEntryAppService)
    {
        _journalEntryAppService = journalEntryAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public JournalEntryDto Entry { get; set; } = null!;
    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Accounting.Edit);
        Entry = await _journalEntryAppService.GetAsync(Id);
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await _journalEntryAppService.DeleteAsync(Id);
        return RedirectToPage("./Index");
    }
}
